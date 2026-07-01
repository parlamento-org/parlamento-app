using Parlamento.Application.Imports;

namespace Parlamento.Application.Abstractions;

public interface IParliamentDocumentRedactionService
{
    Task<ParliamentDocumentRedactionResult> ProcessInitiativeTextDocumentsAsync(
        string? legislature,
        int? projectLawId,
        int maxDocuments,
        bool forceUpsert = false,
        CancellationToken cancellationToken = default);
}
