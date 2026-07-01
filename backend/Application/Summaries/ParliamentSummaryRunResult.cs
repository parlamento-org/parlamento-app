namespace Parlamento.Application.Summaries;

public record ParliamentSummaryRunResult(
    int DocumentsRead,
    int SummariesGenerated,
    int SummariesSkipped,
    int SummariesFailed);
