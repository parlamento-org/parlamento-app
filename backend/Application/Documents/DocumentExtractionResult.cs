using Parlamento.Domain.Documents;

namespace Parlamento.Application.Documents;

public record DocumentExtractionResult(
    ParliamentDocumentModel Document,
    string ExtractorKind,
    string ExtractorVersion);
