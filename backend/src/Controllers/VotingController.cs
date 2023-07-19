using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/vote")]
public class VotingController : ControllerBase
{
    private readonly DbSet<ProjectLaw> _dbProjectLawSet;

    private readonly DbSet<User> _dbUserSet;

    private readonly DbSet<PoliticalParty> _dbPoliticalPartySet;

    private readonly DatabaseContext _context;


    public VotingController(DatabaseContext context)
    {
        this._context = context;
        this._dbUserSet = _context.Set<User>();
        this._dbProjectLawSet = _context.Set<ProjectLaw>();
        this._dbPoliticalPartySet = _context.Set<PoliticalParty>();


    }


    [HttpPut(Name = "GetProposalWithFilters")]
    public IActionResult Get(ProjectLawCriteria criteria)
    {

        var projectLawQuery = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
                .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
                .Include(proposal => proposal.ProposingParty).AsQueryable();


        if (criteria.legislatura != null)
        {
            projectLawQuery = projectLawQuery.Where(proposal => criteria.legislatura.Contains(proposal.Legislatura!));
        }

        if (criteria.oldestVoteDate != null)
        {
            projectLawQuery = projectLawQuery.Where(proposal => proposal.VoteDate!.CompareTo(criteria.oldestVoteDate) >= 0);
        }

        if (criteria.newestVoteDate != null)
        {
            projectLawQuery = projectLawQuery.Where(proposal => proposal.VoteDate!.CompareTo(criteria.newestVoteDate) <= 0);
        }

        projectLawQuery = projectLawQuery.Where(proposal => proposal.Score >= criteria.lowestScoreAllowed);

        var user = _dbUserSet.Include("Votes").FirstOrDefault(x => x.Id == criteria.userID);
        var projectLawList = projectLawQuery.ToList();

        //filter proposals that the user has already voted on
        if (user != null)
        {
            projectLawList.RemoveAll(proposal => user.Votes.Any(x => x.ProjectLawID == proposal.Id));

        }
        if (projectLawQuery.Count() == 0)
        {
            return NotFound("No ProjectLaw found with the given criteria.");
        }

        //return a random proposal from the filtered list, but attribute a higher chance to proposals with higher score
        var random = new Random();
        var proposals = projectLawList;
        var totalScore = proposals.Sum(proposal => proposal.Score);
        var randomScore = random.Next(totalScore);
        var currentScore = 0;
        foreach (var proposal in proposals)
        {
            currentScore += proposal.Score;
            if (currentScore >= randomScore)
            {
                return Ok(proposal);
            }
        }
        return Ok(proposals.Last());


    }

    [HttpPost(Name = "Vote")]
    public async Task<IActionResult> Post(VoteDTO voteDTO)
    {
        var projectLawQuery = _dbProjectLawSet.Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
                .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
                .Include(proposal => proposal.ProposingParty).Where(x => x.Id == voteDTO.projectLawID);

        if (!projectLawQuery.Any())
        {
            return NotFound("No ProjectLaw found with the given id.");
        }

        var projectLaw = projectLawQuery.First();

        var user = _dbUserSet.Include("Votes").Include("PartyStats.PoliticalParty")
        .FirstOrDefault(x => x.Id == voteDTO.userID);

        if (user == null)
        {
            return NotFound("No User found with the given id.");
        }



        var vote = new Vote
        {
            ProjectLawID = voteDTO.projectLawID,
            VotingOrientation = voteDTO.votingOrientation,
            VoteDate = DateTime.Now,
        };

        projectLaw.totalAmountOfVotesFromUsers += 1;

        //update the project law score
        switch (vote.VotingOrientation)
        {
            case VotingOrientation.NotInterested:

                break;
            default:
                projectLaw.amountOfUsersInterested += 1;
                break;
        }

        projectLaw.Score = (projectLaw.amountOfUsersInterested / projectLaw.totalAmountOfVotesFromUsers) * 100;

        //update the user party stats, granting +1 affectionPoint to parties that voted the same way as the user
        foreach (var votingBlock in projectLaw.VotingResultGenerality!.votingBlocks!)
        {

            bool partyStatExists = true;
            var partyStat = user.PartyStats.FirstOrDefault(x => x.PoliticalParty!.partyAcronym == votingBlock.politicalPartyAcronym);
            if (partyStat == null)
            {
                partyStat = new PartyStats
                {
                    PoliticalParty = _dbPoliticalPartySet.FirstOrDefault(x => x.partyAcronym == votingBlock.politicalPartyAcronym)!,
                    totalAffectionPoints = 0,
                    totalAmountOfProposalsVotedOn = 0,
                    PartyAffectionScore = 0
                };
                partyStatExists = false;
            }
            partyStat.totalAmountOfProposalsVotedOn += 1;
            if (partyStat.PoliticalParty == projectLaw.ProposingParty)
            {
                if (votingBlock.votingOrientation == vote.VotingOrientation) partyStat.totalAffectionPoints += 1;
                else partyStat.totalAffectionPoints -= 0.2;
            }
            else if (votingBlock.votingOrientation == vote.VotingOrientation)
            {
                partyStat.totalAffectionPoints += 0.9;
            }
            else
            {
                partyStat.totalAffectionPoints -= 0.1;
            }

            if (partyStat.totalAffectionPoints < 0) partyStat.totalAffectionPoints = 0;

            partyStat.PartyAffectionScore = (partyStat.totalAffectionPoints / partyStat.totalAmountOfProposalsVotedOn) * 100;

            if (!partyStatExists)
            {
                user.PartyStats.Add(partyStat);
            }
        }


        user.Votes.Add(vote);

        await _context.SaveChangesAsync();

        return Ok(user);
    }

}