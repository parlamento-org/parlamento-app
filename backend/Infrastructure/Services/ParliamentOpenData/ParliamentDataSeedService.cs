using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Imports;
using Parlamento.Application.Summaries;
using Parlamento.Application.Topics;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services.ProposalTopics;
using Parlamento.Infrastructure.Services.Summaries;

namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

public class ParliamentDataSeedService : IParliamentDataSeedService
{
    private readonly DatabaseContext _context;
    private readonly IParliamentOpenDataImportService _importService;
    private readonly IParliamentBaseInfoImportService _baseInfoImportService;
    private readonly IParliamentDocumentRedactionService _redactionService;
    private readonly IParliamentSummaryService _summaryService;
    private readonly IProposalTopicTaxonomyImportService _topicTaxonomyImportService;
    private readonly IProjectLawTopicAssignmentService _topicAssignmentService;
    private readonly IOptionsMonitor<ParliamentOpenDataOptions> _openDataOptions;
    private readonly IOptionsMonitor<OpenAiSummaryOptions> _openAiOptions;
    private readonly IOptionsMonitor<ProposalTopicOptions> _topicOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParliamentDataSeedService> _logger;

    public ParliamentDataSeedService(
        DatabaseContext context,
        IParliamentOpenDataImportService importService,
        IParliamentBaseInfoImportService baseInfoImportService,
        IParliamentDocumentRedactionService redactionService,
        IParliamentSummaryService summaryService,
        IProposalTopicTaxonomyImportService topicTaxonomyImportService,
        IProjectLawTopicAssignmentService topicAssignmentService,
        IOptionsMonitor<ParliamentOpenDataOptions> openDataOptions,
        IOptionsMonitor<OpenAiSummaryOptions> openAiOptions,
        IOptionsMonitor<ProposalTopicOptions> topicOptions,
        IConfiguration configuration,
        ILogger<ParliamentDataSeedService> logger)
    {
        _context = context;
        _importService = importService;
        _baseInfoImportService = baseInfoImportService;
        _redactionService = redactionService;
        _summaryService = summaryService;
        _topicTaxonomyImportService = topicTaxonomyImportService;
        _topicAssignmentService = topicAssignmentService;
        _openDataOptions = openDataOptions;
        _openAiOptions = openAiOptions;
        _topicOptions = topicOptions;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ParliamentDataSeedRunResult> SeedAsync(
        ParliamentDataSeedRequest request,
        CancellationToken cancellationToken = default)
    {
        var legislatures = ResolveLegislatures(request);
        var maxDocuments = request.MaxDocuments.GetValueOrDefault(int.MaxValue);
        var includeSummaries = request.IncludeSummaries;
        var openAiConfigured = IsOpenAiConfigured();
        var topicAssignmentConfigured = IsTopicAssignmentConfigured();
        var skipSummariesBecauseOpenAiIsNotConfigured = includeSummaries && !openAiConfigured;
        var skipTopicAssignmentsBecauseOpenAiIsNotConfigured = !topicAssignmentConfigured;

        if (skipSummariesBecauseOpenAiIsNotConfigured)
        {
            _logger.LogWarning(
                "Summary generation was requested for seed pipeline, but OPENAI_API_KEY/OpenAI:ApiKey is not configured. The import and redaction phases will still run.");
        }

        if (skipTopicAssignmentsBecauseOpenAiIsNotConfigured)
        {
            _logger.LogWarning(
                "Proposal topic taxonomy import will run, but automatic topic assignment is skipped because OPENAI_API_KEY/ProposalTopics:ApiKey is not configured.");
        }

        var succeededLegislatures = 0;
        var failedLegislatures = 0;
        var importRead = 0;
        var importInserted = 0;
        var importUpdated = 0;
        var importSkipped = 0;
        var importFailed = 0;
        var documentsRead = 0;
        var documentsProcessed = 0;
        var documentsSkipped = 0;
        var documentsFailed = 0;
        var topicParentTopicsRead = 0;
        var topicParentTopicsInserted = 0;
        var topicParentTopicsUpdated = 0;
        var topicSubtopicsRead = 0;
        var topicSubtopicsInserted = 0;
        var topicSubtopicsUpdated = 0;
        var topicSeedAssignmentsRead = 0;
        var topicSeedAssignmentsInserted = 0;
        var topicSeedAssignmentsUpdated = 0;
        var topicSeedAssignmentsSkipped = 0;
        var topicSeedAssignmentsMissingProjectLaws = 0;
        var topicSeedAssignmentsMissingSubtopics = 0;
        var topicAssignmentDocumentsRead = 0;
        var topicAssignmentsCreated = 0;
        var topicAssignmentsSkipped = 0;
        var topicAssignmentsFailed = 0;
        var topicAutoAssigned = 0;
        var topicNeedsReview = 0;
        var topicUnassigned = 0;
        var topicMissingRedactedText = 0;
        var topicPreservedReviewedAssignments = 0;
        var summaryDocumentsRead = 0;
        var summariesGenerated = 0;
        var summariesSkipped = 0;
        var summariesFailed = 0;

        foreach (var legislature in legislatures)
        {
            try
            {
                _logger.LogInformation(
                    "Starting parliament seed pipeline for Legislature={Legislature}. IncludeSummaries={IncludeSummaries} MaxDocuments={MaxDocuments} ForceImport={ForceImport} ForceRedaction={ForceRedaction} ForceSummaries={ForceSummaries}.",
                    legislature,
                    includeSummaries && openAiConfigured,
                    request.MaxDocuments,
                    request.ForceImport,
                    request.ForceRedaction,
                    request.ForceSummaries);

                var importResult = await _importService.ImportLegislatureAsync(
                    legislature,
                    cancellationToken,
                    force: request.ForceImport);
                importRead += importResult.RecordsRead;
                importInserted += importResult.RecordsInserted;
                importUpdated += importResult.RecordsUpdated;
                importSkipped += importResult.RecordsSkipped;
                importFailed += importResult.RecordsFailed;

                await EnsureBaseInfoAsync(legislature, cancellationToken);

                var redactionResult = await _redactionService.ProcessInitiativeTextDocumentsAsync(
                    legislature,
                    null,
                    maxDocuments,
                    request.ForceRedaction,
                    cancellationToken);
                documentsRead += redactionResult.DocumentsRead;
                documentsProcessed += redactionResult.DocumentsProcessed;
                documentsSkipped += redactionResult.DocumentsSkipped;
                documentsFailed += redactionResult.DocumentsFailed;

                var topicImportResult = await _topicTaxonomyImportService.ImportReviewedTaxonomyAsync(cancellationToken);
                topicParentTopicsRead += topicImportResult.ParentTopicsRead;
                topicParentTopicsInserted += topicImportResult.ParentTopicsInserted;
                topicParentTopicsUpdated += topicImportResult.ParentTopicsUpdated;
                topicSubtopicsRead += topicImportResult.SubtopicsRead;
                topicSubtopicsInserted += topicImportResult.SubtopicsInserted;
                topicSubtopicsUpdated += topicImportResult.SubtopicsUpdated;
                topicSeedAssignmentsRead += topicImportResult.SeedAssignmentsRead;
                topicSeedAssignmentsInserted += topicImportResult.SeedAssignmentsInserted;
                topicSeedAssignmentsUpdated += topicImportResult.SeedAssignmentsUpdated;
                topicSeedAssignmentsSkipped += topicImportResult.SeedAssignmentsSkipped;
                topicSeedAssignmentsMissingProjectLaws += topicImportResult.SeedAssignmentsMissingProjectLaws;
                topicSeedAssignmentsMissingSubtopics += topicImportResult.SeedAssignmentsMissingSubtopics;

                if (topicAssignmentConfigured)
                {
                    var topicAssignmentResult = await _topicAssignmentService.AssignMissingAsync(
                        new ProjectLawTopicAssignmentRequest
                        {
                            Legislature = legislature,
                            Force = request.ForceTopicAssignments,
                            MaxDocuments = request.MaxDocuments
                        },
                        cancellationToken);

                    topicAssignmentDocumentsRead += topicAssignmentResult.DocumentsRead;
                    topicAssignmentsCreated += topicAssignmentResult.AssignmentsCreated;
                    topicAssignmentsSkipped += topicAssignmentResult.AssignmentsSkipped;
                    topicAssignmentsFailed += topicAssignmentResult.AssignmentsFailed;
                    topicAutoAssigned += topicAssignmentResult.AutoAssigned;
                    topicNeedsReview += topicAssignmentResult.NeedsReview;
                    topicUnassigned += topicAssignmentResult.Unassigned;
                    topicMissingRedactedText += topicAssignmentResult.MissingRedactedText;
                    topicPreservedReviewedAssignments += topicAssignmentResult.PreservedReviewedAssignments;
                }

                if (includeSummaries && openAiConfigured)
                {
                    var summaryResult = await _summaryService.GenerateSummariesAsync(
                        new ParliamentSummaryRequest
                        {
                            Legislature = legislature,
                            AllUnprocessed = true,
                            Force = request.ForceSummaries,
                            MaxDocuments = request.MaxDocuments
                        },
                        cancellationToken);

                    summaryDocumentsRead += summaryResult.DocumentsRead;
                    summariesGenerated += summaryResult.SummariesGenerated;
                    summariesSkipped += summaryResult.SummariesSkipped;
                    summariesFailed += summaryResult.SummariesFailed;
                }

                succeededLegislatures++;
                _logger.LogInformation("Parliament seed pipeline completed for Legislature={Legislature}.", legislature);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedLegislatures++;
                _logger.LogError(ex, "Parliament seed pipeline failed for Legislature={Legislature}.", legislature);
            }
        }

        return new ParliamentDataSeedRunResult(
            legislatures,
            succeededLegislatures,
            failedLegislatures,
            importRead,
            importInserted,
            importUpdated,
            importSkipped,
            importFailed,
            documentsRead,
            documentsProcessed,
            documentsSkipped,
            documentsFailed,
            topicParentTopicsRead,
            topicParentTopicsInserted,
            topicParentTopicsUpdated,
            topicSubtopicsRead,
            topicSubtopicsInserted,
            topicSubtopicsUpdated,
            topicSeedAssignmentsRead,
            topicSeedAssignmentsInserted,
            topicSeedAssignmentsUpdated,
            topicSeedAssignmentsSkipped,
            topicSeedAssignmentsMissingProjectLaws,
            topicSeedAssignmentsMissingSubtopics,
            topicAssignmentDocumentsRead,
            topicAssignmentsCreated,
            topicAssignmentsSkipped,
            topicAssignmentsFailed,
            topicAutoAssigned,
            topicNeedsReview,
            topicUnassigned,
            topicMissingRedactedText,
            topicPreservedReviewedAssignments,
            topicAssignmentConfigured,
            skipTopicAssignmentsBecauseOpenAiIsNotConfigured,
            summaryDocumentsRead,
            summariesGenerated,
            summariesSkipped,
            summariesFailed,
            includeSummaries,
            skipSummariesBecauseOpenAiIsNotConfigured);
    }

    private IReadOnlyList<string> ResolveLegislatures(ParliamentDataSeedRequest request)
    {
        if (request.Legislatures.Count > 0)
        {
            return request.Legislatures
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var configured = _openDataOptions.CurrentValue.Legislatures.Keys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (configured.Count > 0)
        {
            return configured;
        }

        var latest = _openDataOptions.CurrentValue.LatestLegislature;
        if (!string.IsNullOrWhiteSpace(latest))
        {
            return [latest];
        }

        throw new InvalidOperationException(
            "No legislatures were supplied and no ParliamentOpenData:Legislatures entries are configured.");
    }

    private async Task EnsureBaseInfoAsync(string legislature, CancellationToken cancellationToken)
    {
        var hasDeputies = await _context.ParliamentDeputies.AnyAsync(x => x.Legislature == legislature, cancellationToken);
        var hasGroups = await _context.ParliamentaryGroups.AnyAsync(x => x.Legislature == legislature, cancellationToken);
        var hasTerms = await _context.ParliamentRedactionTerms.AnyAsync(x => x.Legislature == legislature, cancellationToken);

        if (hasDeputies && hasGroups && hasTerms)
        {
            return;
        }

        _logger.LogInformation(
            "Base information is incomplete for {Legislature}. DeputiesLoaded={DeputiesLoaded} GroupsLoaded={GroupsLoaded} TermsLoaded={TermsLoaded}. Importing base-info before document redaction.",
            legislature,
            hasDeputies,
            hasGroups,
            hasTerms);

        var result = await _baseInfoImportService.ImportLegislatureAsync(legislature, cancellationToken);
        _logger.LogInformation(
            "Base-info preflight completed for {Legislature}. Deputies={Deputies} ParliamentaryGroups={Groups} RedactionTerms={Terms}",
            result.Legislature,
            result.DeputiesRead,
            result.ParliamentaryGroupsRead,
            result.RedactionTermsRebuilt);
    }

    private bool IsOpenAiConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["OPENAI_API_KEY"]) ||
               !string.IsNullOrWhiteSpace(_openAiOptions.CurrentValue.ApiKey);
    }

    private bool IsTopicAssignmentConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["OPENAI_API_KEY"]) ||
               !string.IsNullOrWhiteSpace(_topicOptions.CurrentValue.ApiKey);
    }
}
