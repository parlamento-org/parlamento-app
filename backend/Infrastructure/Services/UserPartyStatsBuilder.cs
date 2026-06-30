using Microsoft.EntityFrameworkCore;

using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

internal static class UserPartyStatsBuilder
{
    public static async Task PopulateAsync(User user, DatabaseContext context, CancellationToken cancellationToken)
    {
        if (user.PartyStats.Count > 0)
        {
            return;
        }

        var parties = await context.PoliticalParties
            .OrderBy(party => party.partyAcronym)
            .ToListAsync(cancellationToken);

        foreach (var party in parties)
        {
            user.PartyStats.Add(new PartyStats
            {
                PoliticalParty = party,
                PartyAffectionScore = 0,
                totalAmountOfProposalsVotedOn = 0,
                totalAffectionPoints = 0
            });
        }
    }
}
