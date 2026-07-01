namespace Parlamento.Application.Imports;

public record ParliamentDocumentRedactionResult(
    int DocumentsRead,
    int DocumentsProcessed,
    int DocumentsSkipped,
    int DocumentsFailed);
