using Parlamento.Application.Imports;

namespace Parlamento.Application.Abstractions;

public interface IParliamentDataSeedService
{
    Task<ParliamentDataSeedRunResult> SeedAsync(
        ParliamentDataSeedRequest request,
        CancellationToken cancellationToken = default);
}
