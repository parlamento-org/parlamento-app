using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.ProposalFlow;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("/proposal-flow")]
public sealed class ProposalFlowController : ControllerBase
{
    private readonly IProposalFlowService _proposalFlowService;

    public ProposalFlowController(IProposalFlowService proposalFlowService)
    {
        _proposalFlowService = proposalFlowService;
    }

    [HttpPost("feed", Name = "GetInitiativeFeedCard")]
    public async Task<IActionResult> GetFeedCard(
        InitiativeFeedRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _proposalFlowService.GetNextFeedCardAsync(userId.Value, request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("interactions", Name = "RecordProposalInteraction")]
    public async Task<IActionResult> RecordInteraction(
        ProposalInteractionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _proposalFlowService.RecordInteractionAsync(userId.Value, request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("initiatives/{initiativeId:int}/reveal", Name = "GetProposalReveal")]
    public async Task<IActionResult> GetReveal(
        int initiativeId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _proposalFlowService.GetRevealAsync(userId.Value, initiativeId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("initiatives/{initiativeId:int}/journey", Name = "GetProposalJourney")]
    public async Task<IActionResult> GetJourney(
        int initiativeId,
        CancellationToken cancellationToken)
    {
        var result = await _proposalFlowService.GetJourneyAsync(initiativeId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("history", Name = "GetProposalHistory")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] ProposalHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _proposalFlowService.GetHistoryAsync(userId.Value, request, cancellationToken);
        return this.ToActionResult(result);
    }
}
