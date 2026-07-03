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
                "Only Support, Oppose, and Skip interactions can be recorded through this endpoint.");
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
        ProposalInteractionType.Skip
    ];

    private static readonly ProposalInteractionType[] VoteActions =
    [
        ProposalInteractionType.Support,
        ProposalInteractionType.Oppose
    ];
}
