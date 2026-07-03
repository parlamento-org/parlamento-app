using Parlamento.Application.ProposalFlow;

namespace Parlamento.Application.Abstractions;

public interface IProposalFlowService
{
    Task<ServiceResult<InitiativeFeedCardResponse>> GetNextFeedCardAsync(
        InitiativeFeedRequest request,
        CancellationToken cancellationToken = default);
}
