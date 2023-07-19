using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/party")]
public class PoliticalPartyController : ControllerBase
{
    private readonly DbSet<PoliticalParty> _dbPartySet;
    private readonly DatabaseContext _context;


    public PoliticalPartyController(DatabaseContext context)
    {
        this._context = context;
        this._dbPartySet = _context.Set<PoliticalParty>();


    }

    [HttpGet(Name = "GetParties")]
    public Dictionary<string, List<PoliticalParty>> Get(string? searchString)
    {

        if (!String.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            return new Dictionary<string, List<PoliticalParty>>
            {

                ["parties"] = _dbPartySet.Where(party => party.partyAcronym!.ToLower().Contains(searchString)).ToList()
            };
        }
        else
            return new Dictionary<string, List<PoliticalParty>>
            {

                ["parties"] = _dbPartySet.ToList()
            };
    }

    [HttpPost(Name = "CreateParty")]
    public async Task<IActionResult> Create(PoliticalParty dto)
    {

        PoliticalParty newParty = new PoliticalParty();

        newParty.partyAcronym = dto.partyAcronym;
        newParty.fullName = dto.fullName;
        newParty.logoLink = dto.logoLink;
        var party = _context.PoliticalParties?.FirstOrDefault(x => x.partyAcronym == newParty.partyAcronym);
        if (party != null)
        {
            return NotFound("There is already a Political Party with this name!");
        }
        _dbPartySet.Add(newParty);
        await _context.SaveChangesAsync();
        return Ok(newParty);
    }

    [HttpDelete("{id}", Name = "DeleteParty")]
    public IActionResult Delete(String id)
    {
        var partyQuery = _dbPartySet.Where(x => x.partyAcronym == id);

        if (!partyQuery.Any())
        {
            return NotFound("No Party found with the given abbreviation.");
        }

        var party = partyQuery.First();
        _dbPartySet.Remove(party);

        _context.SaveChanges();

        return Ok(party);
    }



}