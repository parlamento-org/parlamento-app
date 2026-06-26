using Parlamento.Application.Votes;
using Parlamento.Domain.Entities;

namespace Parlamento.Application.Abstractions;

public interface IVotingService
{
    Task<ServiceResult<ProjectLaw>> GetProposalWithFiltersAsync(ProposalFeedRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> RegisterVoteAsync(VoteRequest request, CancellationToken cancellationToken = default);
}
