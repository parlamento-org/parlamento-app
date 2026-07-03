using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.ProposalFlow;

namespace backend.Controllers;

[ApiController]
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
        var result = await _proposalFlowService.GetNextFeedCardAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }
}
