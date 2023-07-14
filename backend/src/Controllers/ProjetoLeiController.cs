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


    [HttpGet("{id}", Name = "GetProposal")]
    public IActionResult Get(int id)
    {

        var projectLawQuery = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
                .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
                .Include(proposal => proposal.ProposingParty).Where(x => x.Id == id);

        if (!projectLawQuery.Any())
        {
            return NotFound("No ProjectLaw found with the given id.");
        }

        var projectLaw = projectLawQuery.First();

        return Ok(projectLaw);
    }

    [HttpGet(Name = "GetProposals")]
    public Dictionary<string, List<ProjectLaw>> Get(string? searchString)
    {

        if (!String.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            return new Dictionary<string, List<ProjectLaw>>
            {

                ["proposals"] = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
                .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
                .Include(proposal => proposal.ProposingParty).
                Where(proposal => proposal.ProposalTitle!
                .ToLower().Contains(searchString) || proposal.SourceId.ToString().Contains(searchString)).ToList()
            };
        }
        else
            return new Dictionary<string, List<ProjectLaw>>
            {

                ["proposals"] = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
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
        if (dto.sourceId == null)
        {
            return StatusCode(400, "SourceId is required!");
        }
        ProjectLaw newProjectLaw = new ProjectLaw();
        newProjectLaw.Score = 100;
        newProjectLaw.amountOfUsersInterested = 0;
        newProjectLaw.totalAmountOfVotesFromUsers = 0;
        newProjectLaw.ProposalTitle = dto.proposalTitle;
        newProjectLaw.FullProposalTextLink = dto.fullProposalTextLink;

        newProjectLaw.ProposingParty = _context.PoliticalParties?.FirstOrDefault(x => x.partyAcronym == dto.proposingPartyAcronym);
        newProjectLaw.VoteDate = dto.voteDate;

        newProjectLaw.ProposalResult = dto.proposalResult;
        newProjectLaw.VotingResultGenerality = dto.votingResultGenerality;
        newProjectLaw.VotingResultSpeciality = dto.votingResultSpeciality;
        newProjectLaw.ProposalTextHTML = dto.proposalTextHTML;

        newProjectLaw.Legislatura = dto.legislatura;
        newProjectLaw.SourceId = dto.sourceId.Value;

        _dbProjectLawSet.Add(newProjectLaw);
        await _context.SaveChangesAsync();
        return Ok(newProjectLaw);
    }

    [HttpPut("{id}", Name = "UpdateProposal")]
    public async Task<IActionResult> Update(int id, ProjectLawDTO dto)
    {

        Console.WriteLine("Updating proposal with id: " + id);
        var projectLaw = _dbProjectLawSet.FirstOrDefault(x => x.SourceId == id);

        if (projectLaw == null)
        {
            return NotFound("No Proposal found with the given id.");
        }
        if (dto.score != null) projectLaw.Score = dto.score.Value;
        if (dto.proposalTitle != null) projectLaw.ProposalTitle = dto.proposalTitle;
        if (dto.fullProposalTextLink != null) projectLaw.FullProposalTextLink = dto.fullProposalTextLink;

        if (dto.proposingPartyAcronym != null)
            projectLaw.ProposingParty = _context.PoliticalParties?.FirstOrDefault(x => x.partyAcronym == dto.proposingPartyAcronym);

        if (dto.voteDate != null) projectLaw.VoteDate = dto.voteDate;

        if (dto.proposalResult != null) projectLaw.ProposalResult = dto.proposalResult;
        if (dto.votingResultGenerality != null) projectLaw.VotingResultGenerality = dto.votingResultGenerality;


        if (dto.votingResultSpeciality != null) projectLaw.VotingResultSpeciality = dto.votingResultSpeciality;
        if (dto.proposalTextHTML != null) projectLaw.ProposalTextHTML = dto.proposalTextHTML;
        if (dto.legislatura != null) projectLaw.Legislatura = dto.legislatura;

        if (dto.sourceId != null) projectLaw.SourceId = dto.sourceId.Value;

        await _context.SaveChangesAsync();
        return Ok(projectLaw);
    }

    [HttpDelete("{id}", Name = "DeleteProposal")]
    public IActionResult Delete(int id)
    {
        var proposalQuery = _dbProjectLawSet.Where(x => x.Id == id);

        if (!proposalQuery.Any())
        {
            return NotFound("No Proposal found with the given id.");
        }

        var proposal = proposalQuery.First();
        _dbProjectLawSet.Remove(proposal);

        _context.SaveChanges();

        return Ok(proposal);
    }

    [HttpDelete(Name = "DeleteAllProposals")]
    public IActionResult Delete()
    {
        _dbProjectLawSet.RemoveRange(_dbProjectLawSet);
        _context.SaveChanges();
        return Ok();
    }




}