using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Votes;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("/vote")]
public class VotingController : ControllerBase
{
    private readonly IVotingService _votingService;

    public VotingController(IVotingService votingService)
    {
        _votingService = votingService;
    }

    [HttpPut(Name = "GetProposalWithFilters")]
    public async Task<IActionResult> Get(ProposalFeedRequest criteria, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        criteria.UserId = userId.Value;
        var result = await _votingService.GetProposalWithFiltersAsync(criteria, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost(Name = "Vote")]
    public async Task<IActionResult> Post(VoteRequest voteRequest, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        voteRequest.UserId = userId.Value;
        var result = await _votingService.RegisterVoteAsync(voteRequest, cancellationToken);
        return this.ToActionResult(result);
    }
}
