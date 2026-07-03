using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.ProposalFlow;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public sealed class ProposalFlowService : IProposalFlowService
{
    private const int ExcerptLength = 900;

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

        var query = _context.ProjectLaws
            .AsNoTracking()
            .AsSplitQuery()
            .Include(initiative => initiative.Summaries)
            .Include(initiative => initiative.ImportedDocuments)
                .ThenInclude(document => document.Content)
            .Where(initiative => !votedInitiativeIds.Contains(initiative.Id));

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
}
