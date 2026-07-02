namespace Parlamento.Application.Imports;

public record ParliamentDataSeedRunResult(
    IReadOnlyList<string> Legislatures,
    int LegislaturesSucceeded,
    int LegislaturesFailed,
    int ImportRecordsRead,
    int ImportRecordsInserted,
    int ImportRecordsUpdated,
    int ImportRecordsSkipped,
    int ImportRecordsFailed,
    int DocumentsRead,
    int DocumentsProcessed,
    int DocumentsSkipped,
    int DocumentsFailed,
    int SummaryDocumentsRead,
    int SummariesGenerated,
    int SummariesSkipped,
    int SummariesFailed,
    bool SummariesRequested,
    bool SummariesSkippedBecauseOpenAiIsNotConfigured);
