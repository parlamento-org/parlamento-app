using System.Globalization;
using System.Text;
using System.Text.Json;
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
        int userId,
        InitiativeFeedRequest request,
        CancellationToken cancellationToken = default)
    {
        var votedInitiativeIds = await _context.Users
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Votes.Select(vote => vote.ProjectLawID))
            .ToListAsync(cancellationToken);

        var skipExclusionStart = DateTime.UtcNow.Subtract(SkipExclusionWindow);
        var interactedInitiativeIds = await _context.ProposalInteractionEvents
            .Where(interaction =>
                interaction.UserId == userId &&
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
                "Não foram encontradas iniciativas elegíveis para este utilizador.");
        }

        var selected = candidates[Random.Shared.Next(candidates.Count)];
        return ServiceResult<InitiativeFeedCardResponse>.Success(MapFeedCard(selected));
    }

    public async Task<ServiceResult<ProposalInteractionResponse>> RecordInteractionAsync(
        int userId,
        ProposalInteractionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TerminalFeedActions.Contains(request.Action))
        {
            return ServiceResult<ProposalInteractionResponse>.Failure(
                400,
                "Só é possível registar interações de apoio, oposição, abstenção e salto neste endpoint.");
        }

        var userExists = await _context.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);
        if (!userExists)
        {
            return ServiceResult<ProposalInteractionResponse>.Failure(404, "Não foi encontrado nenhum utilizador com o id indicado.");
        }

        var initiativeExists = await _context.ProjectLaws
            .AnyAsync(initiative => initiative.Id == request.InitiativeId, cancellationToken);
        if (!initiativeExists)
        {
            return ServiceResult<ProposalInteractionResponse>.Failure(404, "Não foi encontrada nenhuma iniciativa com o id indicado.");
        }

        var duplicate = await FindDuplicateInteractionAsync(userId, request, cancellationToken);
        if (duplicate != null)
        {
            return ServiceResult<ProposalInteractionResponse>.Success(MapInteraction(duplicate, isDuplicate: true));
        }

        var interaction = new ProposalInteractionEvent
        {
            UserId = userId,
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
                "Os resultados ficam disponíveis depois de votares na iniciativa.");
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
            return ServiceResult<ProposalRevealResponse>.Failure(404, "Não foi encontrada nenhuma iniciativa com o id indicado.");
        }

        return ServiceResult<ProposalRevealResponse>.Success(MapReveal(initiative, userVote.InteractionType));
    }

    public async Task<ServiceResult<ProposalJourneyResponse>> GetJourneyAsync(
        int initiativeId,
        CancellationToken cancellationToken = default)
    {
        var initiative = await _context.ProjectLaws
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.ImportedEvents)
                .ThenInclude(item => item.Votes)
                    .ThenInclude(vote => vote.Blocks)
            .Include(item => item.ImportedEvents)
                .ThenInclude(item => item.Documents)
            .Include(item => item.ImportedEvents)
                .ThenInclude(item => item.Publications)
            .Include(item => item.ImportedEvents)
                .ThenInclude(item => item.Interventions)
            .Include(item => item.ImportedDocuments)
            .Include(item => item.ImportedPublications)
            .Include(item => item.ImportedInterventions)
            .Include(item => item.ImportedVotes)
                .ThenInclude(vote => vote.Blocks)
            .Include(item => item.Summaries)
            .FirstOrDefaultAsync(item => item.Id == initiativeId, cancellationToken);

        if (initiative == null)
        {
            return ServiceResult<ProposalJourneyResponse>.Failure(404, "Não foi encontrada nenhuma iniciativa com o id indicado.");
        }

        return ServiceResult<ProposalJourneyResponse>.Success(MapJourney(initiative));
    }

    public async Task<ServiceResult<ProposalHistoryPageResponse>> GetHistoryAsync(
        int userId,
        ProposalHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var filters = request.ToFilters();

        var availableLegislatures = await _context.ProposalInteractionEvents
            .AsNoTracking()
            .Where(interaction =>
                interaction.UserId == userId &&
                TerminalFeedActions.Contains(interaction.InteractionType) &&
                interaction.ProjectLaw != null &&
                interaction.ProjectLaw.Legislatura != null &&
                interaction.ProjectLaw.Legislatura != string.Empty)
            .Select(interaction => interaction.ProjectLaw!.Legislatura!)
            .Distinct()
            .OrderByDescending(item => item)
            .ToListAsync(cancellationToken);

        var availableProposingPartyRows = await _context.ProposalInteractionEvents
            .AsNoTracking()
            .Where(interaction =>
                interaction.UserId == userId &&
                TerminalFeedActions.Contains(interaction.InteractionType) &&
                interaction.ProjectLaw != null &&
                interaction.ProjectLaw.ProposingParty != null &&
                interaction.ProjectLaw.ProposingParty.partyAcronym != null &&
                interaction.ProjectLaw.ProposingParty.partyAcronym != string.Empty)
            .Select(interaction => new
            {
                Acronym = interaction.ProjectLaw!.ProposingParty!.partyAcronym!,
                Name = interaction.ProjectLaw.ProposingParty.fullName
            })
            .ToListAsync(cancellationToken);

        var availableProposingParties = availableProposingPartyRows
            .GroupBy(item => item.Acronym, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProposalHistoryProposingPartyResponse
            {
                Acronym = group.Key,
                Name = group.Select(item => item.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
            })
            .OrderBy(item => item.Acronym)
            .ToList();

        var filteredInteractions = _context.ProposalInteractionEvents
            .AsNoTracking()
            .Where(interaction =>
                interaction.UserId == userId &&
                TerminalFeedActions.Contains(interaction.InteractionType) &&
                interaction.ProjectLaw != null);

        if (!string.IsNullOrWhiteSpace(filters.Legislature))
        {
            filteredInteractions = filteredInteractions.Where(interaction =>
                interaction.ProjectLaw!.Legislatura == filters.Legislature);
        }

        if (!string.IsNullOrWhiteSpace(filters.ProposingParty))
        {
            filteredInteractions = filteredInteractions.Where(interaction =>
                interaction.ProjectLaw!.ProposingParty != null &&
                interaction.ProjectLaw.ProposingParty.partyAcronym == filters.ProposingParty);
        }

        if (filters.InteractionType is { } interactionType)
        {
            filteredInteractions = filteredInteractions.Where(interaction =>
                interaction.InteractionType == interactionType);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.ToLowerInvariant();
            var normalizedSearch = NormalizeKey(filters.Search);
            var matchingResults = MatchingProposalResults(normalizedSearch);

            filteredInteractions = filteredInteractions.Where(interaction =>
                interaction.ProjectLaw!.ProposalTitle != null &&
                    interaction.ProjectLaw.ProposalTitle.ToLower().Contains(search) ||
                interaction.ProjectLaw!.InitiativeTypeDescription != null &&
                    interaction.ProjectLaw.InitiativeTypeDescription.ToLower().Contains(search) ||
                interaction.ProjectLaw!.InitiativeNumber != null &&
                    interaction.ProjectLaw.InitiativeNumber.ToLower().Contains(search) ||
                interaction.ProjectLaw!.Legislatura != null &&
                    interaction.ProjectLaw.Legislatura.ToLower().Contains(search) ||
                interaction.ProjectLaw!.ProposingParty != null &&
                    interaction.ProjectLaw.ProposingParty.partyAcronym != null &&
                    interaction.ProjectLaw.ProposingParty.partyAcronym.ToLower().Contains(search) ||
                interaction.ProjectLaw!.ProposingParty != null &&
                    interaction.ProjectLaw.ProposingParty.fullName != null &&
                    interaction.ProjectLaw.ProposingParty.fullName.ToLower().Contains(search) ||
                interaction.ProjectLaw!.ProposalResult.HasValue &&
                    matchingResults.Contains(interaction.ProjectLaw.ProposalResult.Value));
        }

        var latestInteractionIds = filteredInteractions
            .GroupBy(interaction => interaction.ProjectLawId)
            .Select(group => group
                .OrderByDescending(interaction => interaction.CreatedAtUtc)
                .ThenByDescending(interaction => interaction.Id)
                .Select(interaction => interaction.Id)
                .First());

        var totalItems = await latestInteractionIds.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        var interactions = await _context.ProposalInteractionEvents
            .AsNoTracking()
            .AsSplitQuery()
            .Include(interaction => interaction.ProjectLaw!)
                .ThenInclude(initiative => initiative.ProposingParty)
            .Include(interaction => interaction.ProjectLaw!)
                .ThenInclude(initiative => initiative.ImportedAuthors)
            .Where(interaction => latestInteractionIds.Contains(interaction.Id))
            .OrderByDescending(interaction => interaction.CreatedAtUtc)
            .ThenByDescending(interaction => interaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = interactions
            .Where(interaction => interaction.ProjectLaw != null)
            .Select(MapHistoryItem)
            .ToList();

        return ServiceResult<ProposalHistoryPageResponse>.Success(new ProposalHistoryPageResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1 && totalPages > 0,
            AvailableLegislatures = availableLegislatures,
            AvailableProposingParties = availableProposingParties
        });
    }

    private static InitiativeFeedCardResponse MapFeedCard(ProjectLaw initiative)
    {
        var summary = initiative.Summaries
            .Where(item => item.GenerationStatus == "Succeeded")
            .OrderByDescending(item => item.GeneratedAtUtc ?? item.CreatedAtUtc)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.SummaryText));

        var redactedContent = initiative.ImportedDocuments
            .Select(document => document.Content)
            .Where(content =>
                content != null &&
                content.RedactionStatus == "Succeeded" &&
                !string.IsNullOrWhiteSpace(content.RedactedContentText))
            .OrderByDescending(content => content!.RedactedAtUtc)
            .FirstOrDefault();
        var redactedText = NormalizeText(redactedContent?.RedactedContentText);

        var title = !string.IsNullOrWhiteSpace(summary?.ShortTitle)
            ? summary.ShortTitle
            : initiative.ProposalTitle;

        return new InitiativeFeedCardResponse
        {
            InitiativeId = initiative.Id,
            InitiativeType = initiative.InitiativeTypeDescription ?? "Iniciativa parlamentar",
            InitiativeNumber = initiative.InitiativeNumber,
            NeutralTitle = title ?? "Iniciativa sem título disponível",
            Summary = summary?.SummaryText,
            SummaryBulletPoints = ParseSummaryBulletPoints(summary?.BulletPointsJson),
            SummaryGeneratedAtUtc = summary?.GeneratedAtUtc,
            RedactedExcerpt = CreateExcerpt(redactedText),
            RedactedText = redactedText,
            RedactedHtml = redactedContent?.RedactedContentHtml,
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
            Title = title ?? initiative.ProposalTitle ?? "Iniciativa sem título disponível",
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

    private static ProposalJourneyResponse MapJourney(ProjectLaw initiative)
    {
        return new ProposalJourneyResponse
        {
            InitiativeId = initiative.Id,
            InitiativeType = initiative.InitiativeTypeDescription ?? "Iniciativa parlamentar",
            InitiativeNumber = initiative.InitiativeNumber,
            Title = initiative.ProposalTitle ?? "Iniciativa sem título disponível",
            Phases = BuildJourneyPhases(initiative)
        };
    }

    private static List<ProposalJourneyPhaseResponse> BuildJourneyPhases(ProjectLaw initiative)
    {
        var eventPhases = initiative.ImportedEvents
            .Select(MapJourneyPhase)
            .ToList();

        if (eventPhases.Count == 0)
        {
            return BuildFallbackJourneyPhases(initiative);
        }

        return eventPhases
            .OrderBy(phase => ParseSortableDate(phase.Date))
            .ThenBy(phase => PhaseSortKey(phase.PhaseCode))
            .ThenBy(phase => phase.PhaseName)
            .ToList();
    }

    private static ProposalJourneyPhaseResponse MapJourneyPhase(ParliamentInitiativeEvent parliamentEvent)
    {
        var votes = parliamentEvent.Votes
            .Select(vote => MapParliamentaryVote(vote, parliamentEvent.PhaseCode, parliamentEvent.PhaseName))
            .ToList();

        return new ProposalJourneyPhaseResponse
        {
            PhaseCode = parliamentEvent.PhaseCode,
            PhaseName = DisplayPhaseName(parliamentEvent.PhaseCode, parliamentEvent.PhaseName),
            Date = parliamentEvent.PhaseDate,
            Status = votes.Select(vote => vote.Result).FirstOrDefault(result => !string.IsNullOrWhiteSpace(result)),
            Summary = PhaseSummary(parliamentEvent.PhaseCode, parliamentEvent.PhaseName),
            Observation = parliamentEvent.Observation,
            ApprovedTextId = parliamentEvent.ApprovedTextId,
            Votes = votes,
            Documents = parliamentEvent.Documents
                .Where(document => !string.IsNullOrWhiteSpace(document.Url))
                .Select(MapDocumentLink)
                .ToList(),
            DiaryLinks = parliamentEvent.Publications
                .Where(publication => !string.IsNullOrWhiteSpace(publication.DiaryUrl))
                .Select(MapDiaryLink)
                .ToList(),
            Videos = parliamentEvent.Interventions
                .Where(intervention => !string.IsNullOrWhiteSpace(intervention.VideoUrl))
                .Select(MapVideo)
                .ToList(),
            Transcripts = parliamentEvent.Interventions
                .Where(intervention => !string.IsNullOrWhiteSpace(intervention.PublicationDiaryUrl))
                .Select(MapTranscriptLink)
                .GroupBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList()
        };
    }

    private static List<ProposalJourneyPhaseResponse> BuildFallbackJourneyPhases(ProjectLaw initiative)
    {
        var phases = new List<ProposalJourneyPhaseResponse>();
        if (initiative.VotingResultGenerality?.votingBlocks is { Count: > 0 })
        {
            phases.Add(new ProposalJourneyPhaseResponse
            {
                PhaseCode = "250",
                PhaseName = "Votação na generalidade",
                Date = initiative.VoteDate,
                Status = MapProposalResult(initiative.ProposalResult),
                Summary = PhaseSummary("250", "Votação na generalidade"),
                Votes = MapGeneralityVote(initiative) is { } vote ? [vote] : []
            });
        }

        var unscopedDocuments = initiative.ImportedDocuments
            .Where(document => document.ParliamentInitiativeEventId == null && !string.IsNullOrWhiteSpace(document.Url))
            .Select(MapDocumentLink)
            .ToList();
        var unscopedPublications = initiative.ImportedPublications
            .Where(publication => publication.ParliamentInitiativeEventId == null && !string.IsNullOrWhiteSpace(publication.DiaryUrl))
            .Select(MapDiaryLink)
            .ToList();

        if (phases.Count == 0 || unscopedDocuments.Count > 0 || unscopedPublications.Count > 0)
        {
            phases.Insert(0, new ProposalJourneyPhaseResponse
            {
                PhaseCode = null,
                PhaseName = "Introdução da iniciativa",
                Date = initiative.ImportedAtUtc?.ToString("yyyy-MM-dd"),
                Summary = "A iniciativa foi apresentada e disponibilizada nas fontes oficiais da Assembleia da República.",
                Documents = unscopedDocuments,
                DiaryLinks = unscopedPublications
            });
        }

        return phases
            .OrderBy(phase => ParseSortableDate(phase.Date))
            .ThenBy(phase => PhaseSortKey(phase.PhaseCode))
            .ThenBy(phase => phase.PhaseName)
            .ToList();
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
            return MapParliamentaryVote(importedVote, "250", "Votação na generalidade");
        }

        if (initiative.VotingResultGenerality?.votingBlocks is not { Count: > 0 } blocks)
        {
            return null;
        }

        return new ParliamentaryVoteSummaryResponse
        {
            StageCode = "250",
            StageName = "Votação na generalidade",
            Date = initiative.VoteDate,
            Result = MapProposalResult(initiative.ProposalResult),
            Approved = initiative.ProposalResult is ProposalResult.ApprovedInGenerality or ProposalResult.ApprovedInSpeciality,
            IsUnanimous = initiative.VotingResultGenerality.isUninamous,
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

    private static ParliamentaryVoteSummaryResponse MapParliamentaryVote(
        ParliamentInitiativeVote vote,
        string? phaseCode,
        string? phaseName)
    {
        return new ParliamentaryVoteSummaryResponse
        {
            StageCode = phaseCode ?? StageCodeFromStage(vote.Stage),
            StageName = DisplayPhaseName(phaseCode, phaseName ?? PhaseNameFromStage(vote.Stage)),
            Date = vote.VoteDate,
            Description = vote.Description,
            Result = vote.Result,
            Approved = IsApproved(vote.Result),
            IsUnanimous = IsUnanimousVote(vote.Unanimous),
            PartyVotes = vote.Blocks
                .Where(block => !IsSyntheticUnanimousBlock(block))
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

    private static List<OfficialSourceLinkResponse> MapOfficialSources(ProjectLaw initiative)
    {
        var sources = new List<OfficialSourceLinkResponse>();
        if (!string.IsNullOrWhiteSpace(initiative.FullProposalTextLink))
        {
            sources.Add(new OfficialSourceLinkResponse
            {
                Kind = "Texto da iniciativa",
                Label = "Texto oficial da iniciativa",
                Url = initiative.FullProposalTextLink
            });
        }

        sources.AddRange(initiative.ImportedDocuments
            .Where(document => !string.IsNullOrWhiteSpace(document.Url))
            .Select(document => new OfficialSourceLinkResponse
            {
                Kind = SourceKindLabel(document.Scope),
                Label = document.Name ?? document.DocumentType ?? "Documento oficial",
                Url = document.Url!
            }));

        sources.AddRange(initiative.ImportedPublications
            .Where(publication => !string.IsNullOrWhiteSpace(publication.DiaryUrl))
            .Select(publication => new OfficialSourceLinkResponse
            {
                Kind = "Diário",
                Label = publication.Type ?? "Diário da Assembleia da República",
                Url = publication.DiaryUrl!
            }));

        return sources
            .GroupBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static OfficialSourceLinkResponse MapDocumentLink(ParliamentInitiativeDocument document)
    {
        return new OfficialSourceLinkResponse
        {
            Kind = SourceKindLabel(document.Scope ?? document.DocumentType),
            Label = document.Name ?? document.DocumentType ?? "Documento oficial",
            Url = document.Url!
        };
    }

    private static OfficialSourceLinkResponse MapDiaryLink(ParliamentInitiativePublication publication)
    {
        return new OfficialSourceLinkResponse
        {
            Kind = "Diário",
            Label = publication.Type ?? "Diário da Assembleia da República",
            Url = publication.DiaryUrl!
        };
    }

    private static OfficialSourceLinkResponse MapTranscriptLink(ParliamentInitiativeIntervention intervention)
    {
        return new OfficialSourceLinkResponse
        {
            Kind = "Transcrição",
            Label = SpeakerLabel(intervention),
            Url = intervention.PublicationDiaryUrl!
        };
    }

    private static ProposalJourneyVideoResponse MapVideo(ParliamentInitiativeIntervention intervention)
    {
        return new ProposalJourneyVideoResponse
        {
            SpeakerName = intervention.SpeakerName,
            SpeakerParty = intervention.SpeakerParty,
            GovernmentMemberName = intervention.GovernmentMemberName,
            GovernmentMemberRole = intervention.GovernmentMemberRole,
            Date = intervention.PlenaryMeetingDate,
            StartTime = intervention.StartTime,
            EndTime = intervention.EndTime,
            Url = intervention.VideoUrl!
        };
    }

    private async Task<ProposalInteractionEvent?> FindDuplicateInteractionAsync(
        int userId,
        ProposalInteractionRequest request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        if (idempotencyKey != null)
        {
            return await _context.ProposalInteractionEvents
                .Where(interaction =>
                    interaction.UserId == userId &&
                    interaction.ProjectLawId == request.InitiativeId &&
                    interaction.IdempotencyKey == idempotencyKey)
                .OrderByDescending(interaction => interaction.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var duplicateWindowStart = DateTime.UtcNow.AddSeconds(-30);
        return await _context.ProposalInteractionEvents
            .Where(interaction =>
                interaction.UserId == userId &&
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

    private static ProposalHistoryItemResponse MapHistoryItem(ProposalInteractionEvent interaction)
    {
        var initiative = interaction.ProjectLaw!;

        return new ProposalHistoryItemResponse
        {
            InteractionId = interaction.Id,
            InitiativeId = initiative.Id,
            InitiativeType = initiative.InitiativeTypeDescription ?? "Iniciativa parlamentar",
            InitiativeNumber = initiative.InitiativeNumber,
            Legislature = initiative.Legislatura,
            Title = initiative.ProposalTitle ?? "Iniciativa sem título disponível",
            Action = interaction.InteractionType,
            CreatedAtUtc = interaction.CreatedAtUtc,
            Proposers = MapProposers(initiative)
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

    private static List<string> ParseSummaryBulletPoints(string? bulletPointsJson)
    {
        if (string.IsNullOrWhiteSpace(bulletPointsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(bulletPointsJson)
                    ?.Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim())
                    .ToList()
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
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
            ProposalResult.ApprovedInGenerality => "Aprovado na generalidade",
            ProposalResult.RejectedInGenerality => "Rejeitado na generalidade",
            ProposalResult.ApprovedInSpeciality => "Aprovado na especialidade",
            ProposalResult.RejectedInSpeciality => "Rejeitado na especialidade",
            _ => null
        };
    }

    private static ProposalResult[] MatchingProposalResults(string normalizedSearch)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return [];
        }

        return Enum.GetValues<ProposalResult>()
            .Where(result =>
                NormalizeKey(result.ToString()).Contains(normalizedSearch) ||
                NormalizeKey(MapProposalResult(result)).Contains(normalizedSearch))
            .ToArray();
    }

    private static DateTime ParseSortableDate(string? value)
    {
        return DateTime.TryParse(value, out var date)
            ? date
            : DateTime.MaxValue;
    }

    private static int PhaseSortKey(string? phaseCode)
    {
        return int.TryParse(phaseCode, out var key) ? key : int.MaxValue;
    }

    private static string DisplayPhaseName(string? phaseCode, string? phaseName)
    {
        return PhaseNameFromCode(phaseCode)
            ?? PhaseNameFromKnownText(phaseName)
            ?? phaseName
            ?? "Fase parlamentar";
    }

    private static string? PhaseNameFromCode(string? phaseCode)
    {
        return phaseCode switch
        {
            "250" => "Votação na generalidade",
            "310" => "Votação na especialidade",
            "320" => "Votação final global",
            "370" or "380" or "390" or "400" => "Fase pós-aprovação",
            "580" => "Publicação",
            _ => null
        };
    }

    private static string? PhaseNameFromKnownText(string? phaseName)
    {
        return NormalizeKey(phaseName) switch
        {
            "votacao na generalidade" => "Votação na generalidade",
            "votacao na especialidade" => "Votação na especialidade",
            "votacao final global" => "Votação final global",
            "baixa comissao especialidade" => "Baixa à comissão para especialidade",
            "baixa a comissao especialidade" => "Baixa à comissão para especialidade",
            "baixa comissao" => "Baixa à comissão",
            "baixa a comissao" => "Baixa à comissão",
            "redacao final" => "Redação final",
            "envio para promulgacao" => "Envio para promulgação",
            "promulgacao" => "Promulgação",
            "fase pos-aprovacao" => "Fase pós-aprovação",
            "publicacao" => "Publicação",
            "publicacao dr" => "Publicação em Diário da República",
            "introducao da iniciativa" => "Introdução da iniciativa",
            _ => null
        };
    }

    private static string PhaseNameFromStage(string? stage)
    {
        return stage switch
        {
            "Generality" => "Votação na generalidade",
            "Speciality" => "Votação na especialidade",
            "FinalGlobal" => "Votação final global",
            "PostApproval" => "Fase pós-aprovação",
            "PublishedLaw" => "Publicação",
            _ => "Votação parlamentar"
        };
    }

    private static string StageCodeFromStage(string? stage)
    {
        return stage switch
        {
            "Generality" => "250",
            "Speciality" => "310",
            "FinalGlobal" => "320",
            "PublishedLaw" => "580",
            _ => string.Empty
        };
    }

    private static string PhaseSummary(string? phaseCode, string? phaseName)
    {
        return PhaseSummaryFromKnownText(phaseName)
            ?? phaseCode switch
            {
                "250" => "A Assembleia vota se concorda, em princípio, com a iniciativa antes de a remeter para trabalho mais detalhado.",
                "310" => "A iniciativa é apreciada na especialidade, normalmente em comissão, onde o texto pode ser alterado artigo a artigo.",
                "320" => "A Assembleia vota o texto final resultante das fases anteriores.",
                "370" or "380" or "390" or "400" => "O texto aprovado segue os atos posteriores necessários antes de poder tornar-se lei.",
                "580" => "O ato final é publicado no canal oficial competente, tornando público o texto aprovado.",
                _ when !string.IsNullOrWhiteSpace(phaseName) => $"{DisplayPhaseName(null, phaseName)}: fase registada no percurso parlamentar da iniciativa.",
                _ => "Fase registada no percurso parlamentar da iniciativa."
            };
    }

    private static string? PhaseSummaryFromKnownText(string? phaseName)
    {
        var normalized = NormalizeKey(phaseName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Contains("admissao") || normalized.Contains("introducao"))
        {
            return "A iniciativa foi apresentada e admitida para tramitação parlamentar.";
        }

        if (normalized.Contains("baixa") && normalized.Contains("comissao") && normalized.Contains("especialidade"))
        {
            return "A iniciativa baixou à comissão competente para apreciação na especialidade, onde o texto pode ser alterado e preparado para votação.";
        }

        if (normalized.Contains("baixa") && normalized.Contains("comissao"))
        {
            return "A iniciativa foi enviada à comissão parlamentar competente para análise e preparação dos trabalhos seguintes.";
        }

        if (normalized.Contains("votacao") && normalized.Contains("generalidade"))
        {
            return "A Assembleia vota se concorda, em princípio, com a iniciativa antes de a remeter para trabalho mais detalhado.";
        }

        if (normalized.Contains("votacao") && normalized.Contains("especialidade"))
        {
            return "A iniciativa é votada na especialidade, normalmente artigo a artigo ou sobre o texto preparado em comissão.";
        }

        if (normalized.Contains("votacao final global"))
        {
            return "A Assembleia vota o texto final resultante das fases anteriores.";
        }

        if (normalized.Contains("redacao final"))
        {
            return "O texto aprovado é revisto para correções formais e consolidação da versão final.";
        }

        if (normalized.Contains("decreto") || normalized.Contains("envio") && normalized.Contains("promulgacao"))
        {
            return "O texto aprovado segue para os atos finais, incluindo envio ao Presidente da República quando aplicável.";
        }

        if (normalized.Contains("promulgacao"))
        {
            return "O Presidente da República decide promulgar o diploma ou exercer o direito de veto.";
        }

        if (normalized.Contains("publicacao"))
        {
            return "O diploma é publicado oficialmente, tornando público o texto final aprovado.";
        }

        if (normalized.Contains("caduc"))
        {
            return "A iniciativa deixou de prosseguir a tramitação parlamentar, normalmente por fim da legislatura ou perda de oportunidade processual.";
        }

        if (normalized.Contains("rejei"))
        {
            return "A iniciativa foi rejeitada e deixou de seguir no processo legislativo.";
        }

        return null;
    }

    private static string SourceKindLabel(string? kind)
    {
        return NormalizeKey(kind) switch
        {
            "initiativetext" or "initiative" => "Texto da iniciativa",
            "diary" or "diario" or "eventpublication" or "publication" => "Diário",
            "transcript" or "transcricao" => "Transcrição",
            "" or "document" or "eventdocument" => "Documento",
            _ => kind ?? "Documento"
        };
    }

    private static bool IsUnanimousVote(string? unanimous)
    {
        return !string.IsNullOrWhiteSpace(unanimous);
    }

    private static bool IsSyntheticUnanimousBlock(ParliamentInitiativeVoteBlock block)
    {
        return string.IsNullOrWhiteSpace(block.PartyAcronym) &&
               string.Equals(block.RawToken, "unanime", StringComparison.OrdinalIgnoreCase);
    }

    private static string SpeakerLabel(ParliamentInitiativeIntervention intervention)
    {
        if (!string.IsNullOrWhiteSpace(intervention.SpeakerName))
        {
            return intervention.SpeakerParty == null
                ? intervention.SpeakerName
                : $"{intervention.SpeakerName} ({intervention.SpeakerParty})";
        }

        if (!string.IsNullOrWhiteSpace(intervention.GovernmentMemberName))
        {
            return intervention.GovernmentMemberRole == null
                ? intervention.GovernmentMemberName
                : $"{intervention.GovernmentMemberName} ({intervention.GovernmentMemberRole})";
        }

        return "Transcrição do debate";
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    private static string NormalizeKey(string? value)
    {
        var normalized = Normalize(value).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return WhitespaceRegex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ");
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
