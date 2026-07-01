using Parlamento.Application.Imports;

namespace Parlamento.Application.Abstractions;

public interface IParliamentOpenDataImportService
{
    Task<ParliamentImportRunResult> ImportLegislatureAsync(
        string legislature,
        CancellationToken cancellationToken = default);

    Task<ParliamentImportRunResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
