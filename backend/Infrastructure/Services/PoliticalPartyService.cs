using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Parties;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public class PoliticalPartyService : IPoliticalPartyService
{
    private readonly DatabaseContext _context;

    public PoliticalPartyService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PoliticalParty>> SearchAsync(string? searchString, CancellationToken cancellationToken = default)
    {
        var query = _context.PoliticalParties.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var loweredSearch = searchString.ToLowerInvariant();
            query = query.Where(party => party.partyAcronym != null && party.partyAcronym.ToLower().Contains(loweredSearch));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<PoliticalParty>> CreateAsync(CreatePoliticalPartyRequest request, CancellationToken cancellationToken = default)
    {
        var party = await _context.PoliticalParties
            .FirstOrDefaultAsync(x => x.partyAcronym == request.PartyAcronym, cancellationToken);

        if (party != null)
        {
            return ServiceResult<PoliticalParty>.Failure(404, "There is already a Political Party with this name!");
        }

        var newParty = new PoliticalParty
        {
            partyAcronym = request.PartyAcronym,
            fullName = request.FullName,
            logoLink = request.LogoLink
        };

        _context.PoliticalParties.Add(newParty);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<PoliticalParty>.Success(newParty);
    }

    public async Task<ServiceResult<PoliticalParty>> DeleteAsync(string partyAcronym, CancellationToken cancellationToken = default)
    {
        var party = await _context.PoliticalParties
            .FirstOrDefaultAsync(x => x.partyAcronym == partyAcronym, cancellationToken);

        if (party == null)
        {
            return ServiceResult<PoliticalParty>.Failure(404, "No Party found with the given abbreviation.");
        }

        _context.PoliticalParties.Remove(party);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<PoliticalParty>.Success(party);
    }
}
