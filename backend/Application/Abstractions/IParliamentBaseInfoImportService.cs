using Parlamento.Application.Imports;

namespace Parlamento.Application.Abstractions;

public interface IParliamentBaseInfoImportService
{
    Task<ParliamentBaseInfoImportResult> ImportLegislatureAsync(
        string legislature,
        CancellationToken cancellationToken = default);

    Task<ParliamentBaseInfoImportResult> ImportFromFileAsync(
        string legislature,
        string filePath,
        CancellationToken cancellationToken = default);
}
