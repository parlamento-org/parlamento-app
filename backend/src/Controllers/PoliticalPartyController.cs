using Microsoft.AspNetCore.Mvc;

using backend.Extensions;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Parties;
using Parlamento.Domain.Entities;

namespace backend.Controllers;

[ApiController]
[Route("/party")]
public class PoliticalPartyController : ControllerBase
{
    private readonly IPoliticalPartyService _politicalPartyService;

    public PoliticalPartyController(IPoliticalPartyService politicalPartyService)
    {
        _politicalPartyService = politicalPartyService;
    }

    [HttpGet(Name = "GetParties")]
    public async Task<Dictionary<string, List<PoliticalParty>>> Get(string? searchString, CancellationToken cancellationToken)
    {
        var parties = await _politicalPartyService.SearchAsync(searchString, cancellationToken);
        return new Dictionary<string, List<PoliticalParty>>
        {
            ["parties"] = parties.ToList()
        };
    }

    [HttpPost(Name = "CreateParty")]
    public async Task<IActionResult> Create(CreatePoliticalPartyRequest request, CancellationToken cancellationToken)
    {
        var result = await _politicalPartyService.CreateAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id}", Name = "DeleteParty")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await _politicalPartyService.DeleteAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }
}
