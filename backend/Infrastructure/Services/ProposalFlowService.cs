using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.ProposalFlow;
using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public sealed class ProposalFlowService : IProposalFlowService
{
    private const int ExcerptLength = 900;
    private static readonly TimeSpan SkipExclusionWindow = TimeSpan.FromDays(14);

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly DatabaseContext _context;

    public ProposalFlowService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<InitiativeFeedCardResponse>> GetNextFeedCardAsync(
        InitiativeFeedRequest request,
        CancellationToken cancellationToken = default)
    {
        var votedInitiativeIds = await _context.Users
            .Where(user => user.Id == request.UserId)
            .SelectMany(user => user.Votes.Select(vote => vote.ProjectLawID))
            .ToListAsync(cancellationToken);

        var skipExclusionStart = DateTime.UtcNow.Subtract(SkipExclusionWindow);
        var interactedInitiativeIds = await _context.ProposalInteractionEvents
            .Where(interaction =>
                interaction.UserId == request.UserId &&
                (VoteActions.Contains(interaction.InteractionType) ||
                 interaction.InteractionType == ProposalInteractionType.Skip &&
                 interaction.CreatedAtUtc >= skipExclusionStart))
            .Select(interaction => interaction.ProjectLawId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var excludedInitiativeIds = votedInitiativeIds
            .Concat(interactedInitiativeIds)
            .Distinct()
            .ToList();

        var query = _context.ProjectLaws
            .AsNoTracking()
            .AsSplitQuery()
            .Include(initiative => initiative.Summaries)
            .Include(initiative => initiative.ImportedDocuments)
                .ThenInclude(document => document.Content)
            .Where(initiative => !excludedInitiativeIds.Contains(initiative.Id));

        if (request.Legislatures is { Count: > 0 })
        {
            query = query.Where(initiative =>
                initiative.Legislatura != null &&
                request.Legislatures.Contains(initiative.Legislatura));
        }

        var candidates = await query
            .OrderByDescending(initiative =>
                initiative.ImportedDocuments.Any(document =>
                    document.Content != null &&
                    document.Content.RedactionStatus == "Succeeded" &&
                    document.Content.RedactedContentText != null))
            .ThenByDescending(initiative =>
                initiative.Summaries.Any(summary =>
                    summary.GenerationStatus == "Succeeded" &&
                    summary.SummaryText != null))
            .ThenByDescending(initiative => initiative.Score)
            .ThenByDescending(initiative => initiative.ImportedAtUtc)
            .Take(30)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return ServiceResult<InitiativeFeedCardResponse>.Failure(
                404,
                "No eligible initiatives found for this user.");
        }

        var selected = candidates[Random.Shared.Next(candidates.Count)];
        return ServiceResult<InitiativeFeedCardResponse>.Success(MapFeedCard(selected));
    }

    public async Task<ServiceResult<ProposalInteractionResponse>> RecordInteractionAsync(
        ProposalInteractionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TerminalFeedActions.Contains(request.Action))
        {
            return ServiceResult<ProposalInteractionResponse>.Failure(
                400,
                "Only Support, Oppose, Abstain, and Skip interactions can be recorded through this endpoint.");
        }

        var userExists = await _context.Users
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return ServiceResult<ProposalInteractionResponse>.Failure(404, "No User found with the given id.");
        }

        var initiativeExists = await _context.ProjectLaws
            .AnyAsync(initiative => initiative.Id == request.InitiativeId, cancellationToken);
        if (!initiativeExists)
        {
            return ServiceResult<ProposalInteractionResponse>.Failure(404, "No initiative found with the given id.");
        }

        var duplicate = await FindDuplicateInteractionAsync(request, cancellationToken);
        if (duplicate != null)
        {
            return ServiceResult<ProposalInteractionResponse>.Success(MapInteraction(duplicate, isDuplicate: true));
        }

        var interaction = new ProposalInteractionEvent
        {
            UserId = request.UserId,
            ProjectLawId = request.InitiativeId,
            InteractionType = request.Action,
            IdempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ProposalInteractionEvents.Add(interaction);
        await IncrementStatsAsync(request.InitiativeId, request.Action, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProposalInteractionResponse>.Success(MapInteraction(interaction, isDuplicate: false));
    }

    public async Task<ServiceResult<ProposalRevealResponse>> GetRevealAsync(
        int userId,
        int initiativeId,
        CancellationToken cancellationToken = default)
    {
        var userVote = await _context.ProposalInteractionEvents
            .AsNoTracking()
            .Where(interaction =>
                interaction.UserId == userId &&
                interaction.ProjectLawId == initiativeId &&
                VoteActions.Contains(interaction.InteractionType))
            .OrderByDescending(interaction => interaction.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (userVote == null)
        {
            return ServiceResult<ProposalRevealResponse>.Failure(
                403,
                "Reveal data is available after the user votes on the initiative.");
        }

        var initiative = await _context.ProjectLaws
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.ProposingParty)
            .Include(item => item.VotingResultGenerality!.votingBlocks)
            .Include(item => item.ImportedAuthors)
            .Include(item => item.ImportedVotes)
                .ThenInclude(vote => vote.Blocks)
            .Include(item => item.ImportedDocuments)
            .Include(item => item.ImportedPublications)
            .Include(item => item.Summaries)
            .FirstOrDefaultAsync(item => item.Id == initiativeId, cancellationToken);

        if (initiative == null)
        {
            return ServiceResult<ProposalRevealResponse>.Failure(404, "No initiative found with the given id.");
        }

        return ServiceResult<ProposalRevealResponse>.Success(MapReveal(initiative, userVote.InteractionType));
    }

    private static InitiativeFeedCardResponse MapFeedCard(ProjectLaw initiative)
    {
        var summary = initiative.Summaries
            .Where(item => item.GenerationStatus == "Succeeded")
            .OrderByDescending(item => item.GeneratedAtUtc ?? item.CreatedAtUtc)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.SummaryText));

        var redactedText = initiative.ImportedDocuments
            .Select(document => document.Content)
            .Where(content =>
                content != null &&
                content.RedactionStatus == "Succeeded" &&
                !string.IsNullOrWhiteSpace(content.RedactedContentText))
            .OrderByDescending(content => content!.RedactedAtUtc)
            .Select(content => NormalizeText(content!.RedactedContentText))
            .FirstOrDefault();

        var title = !string.IsNullOrWhiteSpace(summary?.ShortTitle)
            ? summary.ShortTitle
            : initiative.ProposalTitle;

        return new InitiativeFeedCardResponse
        {
            InitiativeId = initiative.Id,
            InitiativeType = initiative.InitiativeTypeDescription ?? "Iniciativa parlamentar",
            InitiativeNumber = initiative.InitiativeNumber,
            NeutralTitle = title ?? "Iniciativa sem titulo disponivel",
            Summary = summary?.SummaryText,
            SummaryGeneratedAtUtc = summary?.GeneratedAtUtc,
            RedactedExcerpt = CreateExcerpt(redactedText),
            RedactedText = redactedText,
            Legislature = initiative.Legislatura,
            Date = FirstNonEmpty(initiative.VoteDate, initiative.ImportedAtUtc?.ToString("yyyy-MM-dd"))
        };
    }

    private static ProposalRevealResponse MapReveal(
        ProjectLaw initiative,
        ProposalInteractionType userVote)
    {
        var title = initiative.Summaries
            .Where(summary => summary.GenerationStatus == "Succeeded")
            .OrderByDescending(summary => summary.GeneratedAtUtc ?? summary.CreatedAtUtc)
            .Select(summary => summary.ShortTitle)
            .FirstOrDefault(title => !string.IsNullOrWhiteSpace(title));

        return new ProposalRevealResponse
        {
            InitiativeId = initiative.Id,
            InitiativeType = initiative.InitiativeTypeDescription ?? "Iniciativa parlamentar",
            InitiativeNumber = initiative.InitiativeNumber,
            Title = title ?? initiative.ProposalTitle ?? "Iniciativa sem titulo disponivel",
            UserVote = userVote,
            Proposers = MapProposers(initiative),
            GeneralityVote = MapGeneralityVote(initiative),
            OfficialSources = MapOfficialSources(initiative),
            Journey = new ProposalJourneyActionResponse
            {
                Endpoint = $"/proposal-flow/initiatives/{initiative.Id}/journey"
            }
        };
    }

    private static List<ProposalProposerResponse> MapProposers(ProjectLaw initiative)
    {
        var importedAuthors = initiative.ImportedAuthors
            .Select(author => new ProposalProposerResponse
            {
                Kind = author.AuthorKind,
                Name = author.Name,
                Acronym = author.Acronym
            })
            .Where(author =>
                !string.IsNullOrWhiteSpace(author.Name) ||
                !string.IsNullOrWhiteSpace(author.Acronym))
            .ToList();

        if (importedAuthors.Count > 0)
        {
            return importedAuthors;
        }

        return initiative.ProposingParty == null
            ? []
            :
            [
                new ProposalProposerResponse
                {
                    Kind = "PoliticalParty",
                    Name = initiative.ProposingParty.fullName,
                    Acronym = initiative.ProposingParty.partyAcronym
                }
            ];
    }

    private static ParliamentaryVoteSummaryResponse? MapGeneralityVote(ProjectLaw initiative)
    {
        var importedVote = initiative.ImportedVotes
            .Where(vote => vote.Stage == "Generality")
            .OrderByDescending(vote => vote.VoteDate)
            .ThenByDescending(vote => vote.Id)
            .FirstOrDefault();

        if (importedVote != null)
        {
            return new ParliamentaryVoteSummaryResponse
            {
                StageCode = "250",
                StageName = "Votacao na generalidade",
                Date = importedVote.VoteDate,
                Description = importedVote.Description,
                Result = importedVote.Result,
                Approved = IsApproved(importedVote.Result),
                PartyVotes = importedVote.Blocks
                    .Select(block => new PartyVoteResponse
                    {
                        PartyAcronym = block.PartyAcronym ?? block.RawToken ?? string.Empty,
                        Orientation = block.VotingOrientation,
                        NumberOfDeputies = block.NumberOfDeputies,
                        IsUnanimousWithinParty = block.IsUnanimousWithinParty
                    })
                    .Where(block => !string.IsNullOrWhiteSpace(block.PartyAcronym))
                    .ToList()
            };
        }

        if (initiative.VotingResultGenerality?.votingBlocks is not { Count: > 0 } blocks)
        {
            return null;
        }

        return new ParliamentaryVoteSummaryResponse
        {
            StageCode = "250",
            StageName = "Votacao na generalidade",
            Date = initiative.VoteDate,
            Result = MapProposalResult(initiative.ProposalResult),
            Approved = initiative.ProposalResult is ProposalResult.ApprovedInGenerality or ProposalResult.ApprovedInSpeciality,
            PartyVotes = blocks
                .Select(block => new PartyVoteResponse
                {
                    PartyAcronym = block.politicalPartyAcronym ?? string.Empty,
                    Orientation = block.votingOrientation,
                    NumberOfDeputies = block.numberOfDeputies,
                    IsUnanimousWithinParty = block.isUninamousWithinParty
                })
                .Where(block => !string.IsNullOrWhiteSpace(block.PartyAcronym))
                .ToList()
        };
    }

    private static List<OfficialSourceLinkResponse> MapOfficialSources(ProjectLaw initiative)
    {
        var sources = new List<OfficialSourceLinkResponse>();
        if (!string.IsNullOrWhiteSpace(initiative.FullProposalTextLink))
        {
            sources.Add(new OfficialSourceLinkResponse
            {
                Kind = "InitiativeText",
                Label = "Official initiative text",
                Url = initiative.FullProposalTextLink
            });
        }

        sources.AddRange(initiative.ImportedDocuments
            .Where(document => !string.IsNullOrWhiteSpace(document.Url))
            .Select(document => new OfficialSourceLinkResponse
            {
                Kind = document.Scope,
                Label = document.Name ?? document.DocumentType ?? "Official document",
                Url = document.Url!
            }));

        sources.AddRange(initiative.ImportedPublications
            .Where(publication => !string.IsNullOrWhiteSpace(publication.DiaryUrl))
            .Select(publication => new OfficialSourceLinkResponse
            {
                Kind = "Diary",
                Label = publication.Type ?? "Diario da Assembleia da Republica",
                Url = publication.DiaryUrl!
            }));

        return sources
            .GroupBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<ProposalInteractionEvent?> FindDuplicateInteractionAsync(
        ProposalInteractionRequest request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        if (idempotencyKey != null)
        {
            return await _context.ProposalInteractionEvents
                .Where(interaction =>
                    interaction.UserId == request.UserId &&
                    interaction.ProjectLawId == request.InitiativeId &&
                    interaction.IdempotencyKey == idempotencyKey)
                .OrderByDescending(interaction => interaction.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var duplicateWindowStart = DateTime.UtcNow.AddSeconds(-30);
        return await _context.ProposalInteractionEvents
            .Where(interaction =>
                interaction.UserId == request.UserId &&
                interaction.ProjectLawId == request.InitiativeId &&
                interaction.InteractionType == request.Action &&
                interaction.CreatedAtUtc >= duplicateWindowStart)
            .OrderByDescending(interaction => interaction.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task IncrementStatsAsync(
        int initiativeId,
        ProposalInteractionType interactionType,
        CancellationToken cancellationToken)
    {
        var stats = await _context.ProjectLawInteractionStats
            .FirstOrDefaultAsync(item => item.ProjectLawId == initiativeId, cancellationToken);

        if (stats == null)
        {
            stats = new ProjectLawInteractionStats { ProjectLawId = initiativeId };
            _context.ProjectLawInteractionStats.Add(stats);
        }

        switch (interactionType)
        {
            case ProposalInteractionType.Support:
                stats.SupportVotes += 1;
                break;
            case ProposalInteractionType.Oppose:
                stats.OpposeVotes += 1;
                break;
            case ProposalInteractionType.Abstain:
                stats.AbstainVotes += 1;
                break;
            case ProposalInteractionType.Skip:
                stats.Skips += 1;
                break;
            case ProposalInteractionType.Impression:
                stats.Impressions += 1;
                break;
            case ProposalInteractionType.DetailOpen:
                stats.DetailOpens += 1;
                break;
            case ProposalInteractionType.SourceLinkClick:
                stats.SourceLinkClicks += 1;
                break;
            case ProposalInteractionType.DebateLinkClick:
                stats.DebateLinkClicks += 1;
                break;
            case ProposalInteractionType.PostVoteReveal:
                stats.PostVoteReveals += 1;
                break;
        }

        stats.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static ProposalInteractionResponse MapInteraction(
        ProposalInteractionEvent interaction,
        bool isDuplicate)
    {
        return new ProposalInteractionResponse
        {
            InteractionId = interaction.Id,
            UserId = interaction.UserId,
            InitiativeId = interaction.ProjectLawId,
            Action = interaction.InteractionType,
            CreatedAtUtc = interaction.CreatedAtUtc,
            IsDuplicate = isDuplicate
        };
    }

    private static string? CreateExcerpt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Length <= ExcerptLength
            ? text
            : text[..ExcerptLength].TrimEnd() + "...";
    }

    private static string? NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return WhitespaceRegex.Replace(text, " ").Trim();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static bool? IsApproved(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        return Normalize(result) is "aprovado" or "aprovada";
    }

    private static string? MapProposalResult(ProposalResult? result)
    {
        return result switch
        {
            ProposalResult.ApprovedInGenerality => "Approved in generality",
            ProposalResult.RejectedInGenerality => "Rejected in generality",
            ProposalResult.ApprovedInSpeciality => "Approved in speciality",
            ProposalResult.RejectedInSpeciality => "Rejected in speciality",
            _ => null
        };
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        return string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();
    }

    private static readonly ProposalInteractionType[] TerminalFeedActions =
    [
        ProposalInteractionType.Support,
        ProposalInteractionType.Oppose,
        ProposalInteractionType.Abstain,
        ProposalInteractionType.Skip
    ];

    private static readonly ProposalInteractionType[] VoteActions =
    [
        ProposalInteractionType.Support,
        ProposalInteractionType.Oppose,
        ProposalInteractionType.Abstain
    ];
}
