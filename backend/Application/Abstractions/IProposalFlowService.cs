using Parlamento.Application.ProposalFlow;

namespace Parlamento.Application.Abstractions;

public interface IProposalFlowService
{
    Task<ServiceResult<InitiativeFeedCardResponse>> GetNextFeedCardAsync(
        int userId,
        InitiativeFeedRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ProposalInteractionResponse>> RecordInteractionAsync(
        int userId,
        ProposalInteractionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ProposalRevealResponse>> GetRevealAsync(
        int userId,
        int initiativeId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ProposalJourneyResponse>> GetJourneyAsync(
        int initiativeId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<ProposalHistoryItemResponse>>> GetHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
