using Parlamento.Application.Documents;

namespace Parlamento.Application.Abstractions;

public interface IDocumentExtractor
{
    string ExtractorKind { get; }

    string ExtractorVersion { get; }

    bool CanExtract(string sourceName);

    bool CanExtract(byte[] bytes, string sourceName);

    Task<DocumentExtractionResult> ExtractAsync(
        byte[] bytes,
        string sourceName,
        CancellationToken cancellationToken = default);
}
