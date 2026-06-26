using Parlamento.Application.Proposals;
using Parlamento.Domain.Entities;

namespace Parlamento.Application.Abstractions;

public interface IProposalService
{
    Task<ServiceResult<ProjectLaw>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectLaw>> GetBySourceIdAsync(int sourceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectLaw>> SearchAsync(string? searchString, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectLaw>> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectLaw>> UpdateAsync(int id, UpdateProposalRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectLaw>> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
