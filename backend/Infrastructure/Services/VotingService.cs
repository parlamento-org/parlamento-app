using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Votes;
using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public class VotingService : IVotingService
{
    private readonly DatabaseContext _context;

    public VotingService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<ProjectLaw>> GetProposalWithFiltersAsync(ProposalFeedRequest request, CancellationToken cancellationToken = default)
    {
        var projectLawQuery = _context.ProjectLaws
            .Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
            .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
            .Include(proposal => proposal.ProposingParty)
            .AsQueryable();

        var legislaturas = request.GetLegislaturas();
        if (legislaturas != null)
        {
            projectLawQuery = projectLawQuery.Where(proposal => legislaturas.Contains(proposal.Legislatura!));
        }

        if (request.OldestVoteDate != null)
        {
            projectLawQuery = projectLawQuery.Where(proposal => proposal.VoteDate!.CompareTo(request.OldestVoteDate) >= 0);
        }

        if (request.NewestVoteDate != null)
        {
            projectLawQuery = projectLawQuery.Where(proposal => proposal.VoteDate!.CompareTo(request.NewestVoteDate) <= 0);
        }

        projectLawQuery = projectLawQuery.Where(proposal => proposal.Score >= request.LowestScoreAllowed);

        var user = await _context.Users
            .Include(existingUser => existingUser.Votes)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        var projectLawList = await projectLawQuery.ToListAsync(cancellationToken);

        if (user != null)
        {
            projectLawList.RemoveAll(proposal => user.Votes.Any(vote => vote.ProjectLawID == proposal.Id));
        }

        if (projectLawList.Count == 0)
        {
            return ServiceResult<ProjectLaw>.Failure(404, "No ProjectLaw found with the given criteria.");
        }

        var random = new Random();
        var totalScore = projectLawList.Sum(proposal => proposal.Score);

        if (totalScore <= 0)
        {
            return ServiceResult<ProjectLaw>.Success(projectLawList[random.Next(projectLawList.Count)]);
        }

        var randomScore = random.Next(totalScore);
        var currentScore = 0;

        foreach (var proposal in projectLawList)
        {
            currentScore += proposal.Score;
            if (currentScore >= randomScore)
            {
                return ServiceResult<ProjectLaw>.Success(proposal);
            }
        }

        return ServiceResult<ProjectLaw>.Success(projectLawList.Last());
    }

    public async Task<ServiceResult<User>> RegisterVoteAsync(VoteRequest request, CancellationToken cancellationToken = default)
    {
        var projectLaw = await _context.ProjectLaws
            .Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
            .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
            .Include(proposal => proposal.ProposingParty)
            .FirstOrDefaultAsync(x => x.Id == request.ProjectLawId, cancellationToken);

        if (projectLaw == null)
        {
            return ServiceResult<User>.Failure(404, "No ProjectLaw found with the given id.");
        }

        var user = await _context.Users
            .Include(existingUser => existingUser.Votes)
            .Include(existingUser => existingUser.PartyStats)
            .ThenInclude(partyStats => partyStats.PoliticalParty)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return ServiceResult<User>.Failure(404, "No User found with the given id.");
        }

        var vote = new Vote
        {
            ProjectLawID = request.ProjectLawId,
            VotingOrientation = request.VotingOrientation,
            VoteDate = DateTime.Now
        };

        projectLaw.totalAmountOfVotesFromUsers += 1;

        switch (vote.VotingOrientation)
        {
            case VotingOrientation.NotInterested:
                break;
            default:
                projectLaw.amountOfUsersInterested += 1;
                break;
        }

        projectLaw.Score = (projectLaw.amountOfUsersInterested / projectLaw.totalAmountOfVotesFromUsers) * 100;

        var votingBlocks = projectLaw.VotingResultGenerality?.votingBlocks ?? new List<VotingBlock>();
        foreach (var votingBlock in votingBlocks)
        {
            var partyStat = user.PartyStats
                .FirstOrDefault(x => x.PoliticalParty!.partyAcronym == votingBlock.politicalPartyAcronym);

            var partyStatExists = partyStat != null;
            if (partyStat == null)
            {
                partyStat = new PartyStats
                {
                    PoliticalParty = await _context.PoliticalParties
                        .FirstOrDefaultAsync(x => x.partyAcronym == votingBlock.politicalPartyAcronym, cancellationToken),
                    totalAffectionPoints = 0,
                    totalAmountOfProposalsVotedOn = 0,
                    PartyAffectionScore = 0
                };
            }

            if (partyStat.PoliticalParty == null)
            {
                continue;
            }

            partyStat.totalAmountOfProposalsVotedOn += 1;

            if (partyStat.PoliticalParty == projectLaw.ProposingParty)
            {
                if (votingBlock.votingOrientation == vote.VotingOrientation)
                {
                    partyStat.totalAffectionPoints += 1;
                }
                else
                {
                    partyStat.totalAffectionPoints -= 0.2;
                }
            }
            else if (votingBlock.votingOrientation == vote.VotingOrientation)
            {
                partyStat.totalAffectionPoints += 0.9;
            }
            else
            {
                partyStat.totalAffectionPoints -= 0.1;
            }

            if (partyStat.totalAffectionPoints < 0)
            {
                partyStat.totalAffectionPoints = 0;
            }

            partyStat.PartyAffectionScore =
                (partyStat.totalAffectionPoints / partyStat.totalAmountOfProposalsVotedOn) * 100;

            if (!partyStatExists)
            {
                user.PartyStats.Add(partyStat);
            }
        }

        user.Votes.Add(vote);

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<User>.Success(user);
    }
}
