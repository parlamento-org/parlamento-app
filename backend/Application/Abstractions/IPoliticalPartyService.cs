using Parlamento.Application.Parties;
using Parlamento.Domain.Entities;

namespace Parlamento.Application.Abstractions;

public interface IPoliticalPartyService
{
    Task<IReadOnlyList<PoliticalParty>> SearchAsync(string? searchString, CancellationToken cancellationToken = default);

    Task<ServiceResult<PoliticalParty>> CreateAsync(CreatePoliticalPartyRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PoliticalParty>> DeleteAsync(string partyAcronym, CancellationToken cancellationToken = default);
}
