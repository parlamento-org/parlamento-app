using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/proposal")]
public class ProjetoLeiController : ControllerBase
{
    private readonly DbSet<ProjectLaw> _dbProjectLawSet;
    private readonly DatabaseContext _context;


    public ProjetoLeiController(DatabaseContext context)
    {
        this._context = context;
        this._dbProjectLawSet = _context.Set<ProjectLaw>();


    }

    [HttpGet(Name = "GetProposals")]
    public Dictionary<string, List<ProjectLaw>> Get(string? searchString)
    {

        if (!String.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            return new Dictionary<string, List<ProjectLaw>>
            {

                ["proposal"] = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
                .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
                .Include(proposal => proposal.ProposingParty).
                Where(proposal => proposal.ProposalTitle!
                .ToLower().Contains(searchString)).ToList()
            };
        }
        else
            return new Dictionary<string, List<ProjectLaw>>
            {

                ["proposal"] = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
                .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
                .Include(proposal => proposal.ProposingParty).ToList()
            };
    }

    [HttpPost(Name = "AddProposal")]
    public async Task<IActionResult> Vote(ProjectLawDTO dto)
    {

        var projectLaw = _dbProjectLawSet.FirstOrDefault(x => x.FullProposalTextLink == dto.fullProposalTextLink);
        if (projectLaw != null)
        {
            return NotFound("This proposal already exists!");
        }

        ProjectLaw newProjectLaw = new ProjectLaw();
        newProjectLaw.Score = 0;
        newProjectLaw.ProposalTitle = dto.proposalTitle;
        newProjectLaw.FullProposalTextLink = dto.fullProposalTextLink;

        newProjectLaw.ProposingParty = _context.PoliticalParties?.FirstOrDefault(x => x.partyAcronym == dto.proposingPartyAcronym);
        newProjectLaw.VoteDate = dto.voteDate;

        newProjectLaw.ProposalResult = dto.proposalResult;
        newProjectLaw.VotingResultGenerality = dto.votingResultGenerality;
        newProjectLaw.VotingResultSpeciality = dto.votingResultSpeciality;

        newProjectLaw.Legislatura = dto.legislatura;
        newProjectLaw.SourceId = dto.sourceId;

        _dbProjectLawSet.Add(newProjectLaw);
        await _context.SaveChangesAsync();
        return Ok(newProjectLaw);
    }

    [HttpDelete("{id}", Name = "DeleteProposal")]
    public IActionResult Delete(int id)
    {
        var proposalQuery = _dbProjectLawSet.Where(x => x.Id == id);

        if (!proposalQuery.Any())
        {
            return NotFound("No Proposal found with the given abbreviation.");
        }

        var proposal = proposalQuery.First();
        _dbProjectLawSet.Remove(proposal);

        _context.SaveChanges();

        return Ok(proposal);
    }



}