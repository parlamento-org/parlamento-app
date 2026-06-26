using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Proposals;
using Parlamento.Domain.Entities;

namespace backend.Controllers;

[ApiController]
[Route("/proposal")]
public class ProjetoLeiController : ControllerBase
{
    private readonly IProposalService _proposalService;

    public ProjetoLeiController(IProposalService proposalService)
    {
        _proposalService = proposalService;
    }

    [HttpGet("{id}", Name = "GetProposal")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await _proposalService.GetByIdAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("source/{sourceId}", Name = "GetProposalBySourceId")]
    public async Task<IActionResult> GetBySourceId(int sourceId, CancellationToken cancellationToken)
    {
        var result = await _proposalService.GetBySourceIdAsync(sourceId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet(Name = "GetProposals")]
    public async Task<Dictionary<string, List<ProjectLaw>>> Get(string? searchString, CancellationToken cancellationToken)
    {
        var proposals = await _proposalService.SearchAsync(searchString, cancellationToken);
        return new Dictionary<string, List<ProjectLaw>>
        {
            ["proposals"] = proposals.ToList()
        };
    }

    [HttpPost(Name = "AddProposal")]
    public async Task<IActionResult> Create(CreateProposalRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{id}", Name = "UpdateProposal")]
    public async Task<IActionResult> Update(int id, UpdateProposalRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.UpdateAsync(id, request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id}", Name = "DeleteProposal")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _proposalService.DeleteAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete(Name = "DeleteAllProposals")]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await _proposalService.DeleteAllAsync(cancellationToken);
        return Ok();
    }
}
