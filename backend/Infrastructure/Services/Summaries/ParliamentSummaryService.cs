using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Summaries;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services.Summaries;

public class ParliamentSummaryService : IParliamentSummaryService
{
    private const int MinimumDocumentCharacters = 300;
    private readonly DatabaseContext _context;
    private readonly ILegislativeSummaryClient _summaryClient;
    private readonly ILogger<ParliamentSummaryService> _logger;

    public ParliamentSummaryService(
        DatabaseContext context,
        ILegislativeSummaryClient summaryClient,
        ILogger<ParliamentSummaryService> logger)
    {
        _context = context;
        _summaryClient = summaryClient;
        _logger = logger;
    }

    public async Task<ParliamentSummaryRunResult> GenerateSummariesAsync(
        ParliamentSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ParliamentDocumentContents
            .Include(x => x.ProjectLaw)
            .Where(x => x.RedactionStatus == "Succeeded")
            .Where(x => !string.IsNullOrWhiteSpace(x.RedactedContentText))
            .Where(x => !string.IsNullOrWhiteSpace(x.RedactedContentHash));

        if (request.ProjectLawId.HasValue)
        {
            query = query.Where(x => x.ProjectLawId == request.ProjectLawId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Legislature))
        {
            query = query.Where(x => x.ProjectLaw != null && x.ProjectLaw.Legislatura == request.Legislature);
        }

        if (request.AllUnprocessed && !request.Force)
        {
            query = query.Where(x => !_context.ParliamentSummaries.Any(summary =>
                summary.ProjectLawId == x.ProjectLawId &&
                summary.SourceDocumentHash == x.RedactedContentHash &&
                summary.ModelName == _summaryClient.ModelName &&
                summary.PromptVersion == _summaryClient.PromptVersion &&
                (summary.GenerationStatus == "Succeeded" || summary.GenerationStatus == "Skipped")));
        }

        query = query.OrderBy(x => x.Id);

        if (request.MaxDocuments is > 0)
        {
            query = query.Take(request.MaxDocuments.Value);
        }

        var documents = await query.ToListAsync(cancellationToken);
        var generated = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var document in documents)
        {
            try
            {
                var outcome = await ProcessDocumentAsync(document, request.Force, cancellationToken);
                switch (outcome)
                {
                    case SummaryOutcome.Generated:
                        generated++;
                        break;
                    case SummaryOutcome.Skipped:
                        skipped++;
                        break;
                    case SummaryOutcome.Failed:
                        failed++;
                        break;
                }
            }
            catch (Exception ex)
            {
                failed++;
                await StoreFailureAsync(document, ex.Message, ex.ToString(), cancellationToken);
                _logger.LogError(
                    ex,
                    "Failed to generate AI summary for ProjectLaw {ProjectLawId} DocumentContent {DocumentContentId}.",
                    document.ProjectLawId,
                    document.Id);
            }
        }

        return new ParliamentSummaryRunResult(documents.Count, generated, skipped, failed);
    }

    private async Task<SummaryOutcome> ProcessDocumentAsync(
        ParliamentDocumentContent document,
        bool force,
        CancellationToken cancellationToken)
    {
        var sourceHash = document.RedactedContentHash!;
        var existing = await FindExistingSummaryAsync(document.ProjectLawId, sourceHash, cancellationToken);
        if (!force && existing is not null && existing.GenerationStatus is "Succeeded" or "Skipped")
        {
            _logger.LogInformation(
                "Skipping AI summary for ProjectLaw {ProjectLawId}; existing {Status} summary is current. Use --force to regenerate.",
                document.ProjectLawId,
                existing.GenerationStatus);
            return SummaryOutcome.Skipped;
        }

        var text = document.RedactedContentText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            await StoreSkippedAsync(document, "Empty redacted document text.", cancellationToken);
            return SummaryOutcome.Skipped;
        }

        if (CountNonWhitespace(text) < MinimumDocumentCharacters)
        {
            await StoreSkippedAsync(
                document,
                $"Redacted document text is shorter than {MinimumDocumentCharacters} non-whitespace characters.",
                cancellationToken);
            return SummaryOutcome.Skipped;
        }

        var generated = await _summaryClient.GenerateSummaryAsync(text, cancellationToken);
        var summary = existing ?? new ParliamentSummary
        {
            ProjectLawId = document.ProjectLawId,
            ParliamentDocumentContentId = document.Id,
            ModelName = _summaryClient.ModelName,
            PromptVersion = _summaryClient.PromptVersion,
            SourceDocumentHash = sourceHash,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (existing is null)
        {
            _context.ParliamentSummaries.Add(summary);
        }

        summary.ProjectLawId = document.ProjectLawId;
        summary.ParliamentDocumentContentId = document.Id;
        summary.ModelName = _summaryClient.ModelName;
        summary.PromptVersion = _summaryClient.PromptVersion;
        summary.SourceDocumentHash = sourceHash;
        summary.ShortTitle = generated.ShortTitle;
        summary.SummaryText = generated.SummaryText;
        summary.BulletPointsJson = JsonSerializer.Serialize(generated.BulletPoints, JsonOptions);
        summary.GenerationStatus = "Succeeded";
        summary.ErrorMessage = null;
        summary.ErrorDetails = null;
        summary.GeneratedAtUtc = DateTime.UtcNow;
        summary.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Generated AI summary for ProjectLaw {ProjectLawId} using Model={Model} PromptVersion={PromptVersion}.",
            document.ProjectLawId,
            _summaryClient.ModelName,
            _summaryClient.PromptVersion);

        return SummaryOutcome.Generated;
    }

    private async Task StoreSkippedAsync(
        ParliamentDocumentContent document,
        string reason,
        CancellationToken cancellationToken)
    {
        var summary = await FindExistingSummaryAsync(document.ProjectLawId, document.RedactedContentHash!, cancellationToken)
                      ?? new ParliamentSummary
                      {
                          ProjectLawId = document.ProjectLawId,
                          ParliamentDocumentContentId = document.Id,
                          ModelName = _summaryClient.ModelName,
                          PromptVersion = _summaryClient.PromptVersion,
                          SourceDocumentHash = document.RedactedContentHash!,
                          CreatedAtUtc = DateTime.UtcNow
                      };

        if (summary.Id == 0)
        {
            _context.ParliamentSummaries.Add(summary);
        }

        summary.GenerationStatus = "Skipped";
        summary.ErrorMessage = reason;
        summary.ErrorDetails = null;
        summary.GeneratedAtUtc = null;
        summary.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Skipped AI summary for ProjectLaw {ProjectLawId}: {Reason}",
            document.ProjectLawId,
            reason);
    }

    private async Task StoreFailureAsync(
        ParliamentDocumentContent document,
        string errorMessage,
        string errorDetails,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.RedactedContentHash))
        {
            return;
        }

        var summary = await FindExistingSummaryAsync(document.ProjectLawId, document.RedactedContentHash, cancellationToken)
                      ?? new ParliamentSummary
                      {
                          ProjectLawId = document.ProjectLawId,
                          ParliamentDocumentContentId = document.Id,
                          ModelName = _summaryClient.ModelName,
                          PromptVersion = _summaryClient.PromptVersion,
                          SourceDocumentHash = document.RedactedContentHash,
                          CreatedAtUtc = DateTime.UtcNow
                      };

        if (summary.Id == 0)
        {
            _context.ParliamentSummaries.Add(summary);
        }

        summary.GenerationStatus = "Failed";
        summary.ErrorMessage = errorMessage;
        summary.ErrorDetails = errorDetails;
        summary.GeneratedAtUtc = null;
        summary.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task<ParliamentSummary?> FindExistingSummaryAsync(
        int projectLawId,
        string sourceHash,
        CancellationToken cancellationToken)
    {
        return _context.ParliamentSummaries
            .Where(x => x.ProjectLawId == projectLawId)
            .Where(x => x.SourceDocumentHash == sourceHash)
            .Where(x => x.ModelName == _summaryClient.ModelName)
            .Where(x => x.PromptVersion == _summaryClient.PromptVersion)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static int CountNonWhitespace(string value)
    {
        return value.Count(x => !char.IsWhiteSpace(x));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private enum SummaryOutcome
    {
        Generated,
        Skipped,
        Failed
    }
}
