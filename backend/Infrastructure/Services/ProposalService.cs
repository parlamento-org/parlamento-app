using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Proposals;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public class ProposalService : IProposalService
{
    private readonly DatabaseContext _context;

    public ProposalService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<ProjectLaw>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var projectLaw = await ProposalsWithDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return projectLaw == null
            ? ServiceResult<ProjectLaw>.Failure(404, "No ProjectLaw found with the given id.")
            : ServiceResult<ProjectLaw>.Success(projectLaw);
    }

    public async Task<ServiceResult<ProjectLaw>> GetBySourceIdAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        var projectLaw = await ProposalsWithDetails()
            .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);

        return projectLaw == null
            ? ServiceResult<ProjectLaw>.Failure(404, "No ProjectLaw found with the given sourceId.")
            : ServiceResult<ProjectLaw>.Success(projectLaw);
    }

    public async Task<ServiceResult<ProjectLaw>> GetBySourceIdTextAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        var projectLaw = await ProposalsWithDetails()
            .FirstOrDefaultAsync(x => x.SourceIdText == sourceId, cancellationToken);

        return projectLaw == null
            ? ServiceResult<ProjectLaw>.Failure(404, "No ProjectLaw found with the given sourceId.")
            : ServiceResult<ProjectLaw>.Success(projectLaw);
    }

    public async Task<IReadOnlyList<ProjectLaw>> SearchAsync(string? searchString, CancellationToken cancellationToken = default)
    {
        var query = ProposalsWithDetails();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var loweredSearch = searchString.ToLowerInvariant();
            query = query.Where(proposal =>
                proposal.ProposalTitle != null && proposal.ProposalTitle.ToLower().Contains(loweredSearch) ||
                proposal.SourceId.ToString().Contains(loweredSearch) ||
                proposal.SourceIdText != null && proposal.SourceIdText.ToLower().Contains(loweredSearch) ||
                proposal.InitiativeNumber != null && proposal.InitiativeNumber.ToLower().Contains(loweredSearch) ||
                proposal.InitiativeTypeDescription != null && proposal.InitiativeTypeDescription.ToLower().Contains(loweredSearch));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<ProjectLaw>> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken = default)
    {
        var existingProposal = await _context.ProjectLaws
            .FirstOrDefaultAsync(x => x.FullProposalTextLink == request.FullProposalTextLink, cancellationToken);

        if (existingProposal != null)
        {
            return ServiceResult<ProjectLaw>.Failure(404, "This proposal already exists!");
        }

        if (!request.SourceId.HasValue)
        {
            return ServiceResult<ProjectLaw>.Failure(400, "SourceId is required!");
        }

        var proposingParty = await FindPartyAsync(request.ProposingPartyAcronym, cancellationToken);
        if (proposingParty == null)
        {
            return ServiceResult<ProjectLaw>.Failure(404, "No Party found with the given abbreviation.");
        }

        var newProjectLaw = new ProjectLaw
        {
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            ProposalTitle = request.ProposalTitle,
            FullProposalTextLink = request.FullProposalTextLink,
            ProposingParty = proposingParty,
            VoteDate = request.VoteDate,
            ProposalResult = request.ProposalResult,
            VotingResultGenerality = request.VotingResultGenerality,
            VotingResultSpeciality = request.VotingResultSpeciality,
            ProposalTextHTML = request.ProposalTextHtml,
            Legislatura = request.Legislatura,
            SourceId = request.SourceId.Value,
            SourceIdText = request.SourceIdText ?? request.SourceId.Value.ToString(),
            InitiativeNumber = request.InitiativeNumber,
            InitiativeTypeCode = request.InitiativeTypeCode,
            InitiativeTypeDescription = request.InitiativeTypeDescription,
            InitiativeSelection = request.InitiativeSelection,
            InitiativeObservations = request.InitiativeObservations,
            InitiativeTextSubstitution = request.InitiativeTextSubstitution,
            InitiativeTextSubstitutionField = request.InitiativeTextSubstitutionField
        };

        _context.ProjectLaws.Add(newProjectLaw);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProjectLaw>.Success(newProjectLaw);
    }

    public async Task<ServiceResult<ProjectLaw>> UpdateAsync(int id, UpdateProposalRequest request, CancellationToken cancellationToken = default)
    {
        var projectLaw = await _context.ProjectLaws
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (projectLaw == null)
        {
            return ServiceResult<ProjectLaw>.Failure(404, "No Proposal found with the given id.");
        }

        if (request.Score.HasValue)
        {
            projectLaw.Score = request.Score.Value;
        }

        if (request.ProposalTitle != null)
        {
            projectLaw.ProposalTitle = request.ProposalTitle;
        }

        if (request.FullProposalTextLink != null)
        {
            projectLaw.FullProposalTextLink = request.FullProposalTextLink;
        }

        if (request.ProposingPartyAcronym != null)
        {
            var proposingParty = await FindPartyAsync(request.ProposingPartyAcronym, cancellationToken);
            if (proposingParty == null)
            {
                return ServiceResult<ProjectLaw>.Failure(404, "No Party found with the given abbreviation.");
            }

            projectLaw.ProposingParty = proposingParty;
        }

        if (request.VoteDate != null)
        {
            projectLaw.VoteDate = request.VoteDate;
        }

        if (request.ProposalResult != null)
        {
            projectLaw.ProposalResult = request.ProposalResult;
        }

        if (request.VotingResultGenerality != null)
        {
            projectLaw.VotingResultGenerality = request.VotingResultGenerality;
        }

        if (request.VotingResultSpeciality != null)
        {
            projectLaw.VotingResultSpeciality = request.VotingResultSpeciality;
        }

        if (request.ProposalTextHtml != null)
        {
            projectLaw.ProposalTextHTML = request.ProposalTextHtml;
        }

        if (request.Legislatura != null)
        {
            projectLaw.Legislatura = request.Legislatura;
        }

        if (request.SourceId.HasValue)
        {
            projectLaw.SourceId = request.SourceId.Value;
            projectLaw.SourceIdText ??= request.SourceId.Value.ToString();
        }

        if (request.SourceIdText != null)
        {
            projectLaw.SourceIdText = request.SourceIdText;
        }

        if (request.InitiativeNumber != null)
        {
            projectLaw.InitiativeNumber = request.InitiativeNumber;
        }

        if (request.InitiativeTypeCode != null)
        {
            projectLaw.InitiativeTypeCode = request.InitiativeTypeCode;
        }

        if (request.InitiativeTypeDescription != null)
        {
            projectLaw.InitiativeTypeDescription = request.InitiativeTypeDescription;
        }

        if (request.InitiativeSelection != null)
        {
            projectLaw.InitiativeSelection = request.InitiativeSelection;
        }

        if (request.InitiativeObservations != null)
        {
            projectLaw.InitiativeObservations = request.InitiativeObservations;
        }

        if (request.InitiativeTextSubstitution != null)
        {
            projectLaw.InitiativeTextSubstitution = request.InitiativeTextSubstitution;
        }

        if (request.InitiativeTextSubstitutionField != null)
        {
            projectLaw.InitiativeTextSubstitutionField = request.InitiativeTextSubstitutionField;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProjectLaw>.Success(projectLaw);
    }

    public async Task<ServiceResult<ProjectLaw>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var proposal = await _context.ProjectLaws
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (proposal == null)
        {
            return ServiceResult<ProjectLaw>.Failure(404, "No Proposal found with the given id.");
        }

        _context.ProjectLaws.Remove(proposal);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProjectLaw>.Success(proposal);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _context.ProjectLaws.RemoveRange(_context.ProjectLaws);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ProjectLaw> ProposalsWithDetails()
    {
        return _context.ProjectLaws
            .AsSplitQuery()
            .Include(proposal => proposal.VotingResultGenerality!.votingBlocks)
            .Include(proposal => proposal.VotingResultSpeciality!.votingBlocks)
            .Include(proposal => proposal.ProposingParty)
            .Include(proposal => proposal.LastImportRun)
            .Include(proposal => proposal.ImportedAuthors)
            .Include(proposal => proposal.ImportedEvents)
            .Include(proposal => proposal.ImportedVotes)
                .ThenInclude(vote => vote.Blocks)
            .Include(proposal => proposal.ImportedDocuments)
            .Include(proposal => proposal.ImportedPublications)
            .Include(proposal => proposal.ImportedInterventions);
    }

    private async Task<PoliticalParty?> FindPartyAsync(string? partyAcronym, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(partyAcronym))
        {
            return null;
        }

        return await _context.PoliticalParties
            .FirstOrDefaultAsync(x => x.partyAcronym == partyAcronym, cancellationToken);
    }
}
