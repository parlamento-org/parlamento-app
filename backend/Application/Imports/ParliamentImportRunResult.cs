namespace Parlamento.Application.Imports;

public record ParliamentImportRunResult(
    int RunId,
    string Status,
    int RecordsRead,
    int RecordsInserted,
    int RecordsUpdated,
    int RecordsSkipped,
    int RecordsFailed,
    string? ErrorMessage);
