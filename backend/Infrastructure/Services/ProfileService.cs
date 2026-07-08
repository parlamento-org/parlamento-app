using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Profile;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public sealed class ProfileService : IProfileService
{
    private const int MinimumComparableVotes = 10;
    private const string GeneralityStage = "Generality";
    private const string GovernmentAcronym = "Governo";

    private static readonly ProposalInteractionType[] TerminalActions =
    [
        ProposalInteractionType.Support,
        ProposalInteractionType.Oppose,
        ProposalInteractionType.Abstain,
        ProposalInteractionType.Skip
    ];

    private static readonly ProposalInteractionType[] UserComparableActions =
    [
        ProposalInteractionType.Support,
        ProposalInteractionType.Oppose,
        ProposalInteractionType.Abstain
    ];

    private static readonly VotingOrientation[] PartyComparableOrientations =
    [
        VotingOrientation.InFavor,
        VotingOrientation.Against,
        VotingOrientation.Abstaining
    ];

    private readonly DatabaseContext _context;

    public ProfileService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<ProfileResponse>> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ServiceResult<ProfileResponse>.Failure(404, "Nao foi encontrado nenhum utilizador com o id indicado.");
        }

        var latestInteractions = await GetLatestTerminalInteractionsAsync(userId, cancellationToken);
        var overview = BuildOverview(latestInteractions);
        var partyAlignment = await BuildPartyAlignmentAsync(latestInteractions, cancellationToken);

        return ServiceResult<ProfileResponse>.Success(new ProfileResponse
        {
            Overview = overview,
            PartyAlignment = partyAlignment
        });
    }

    private async Task<List<LatestInteractionRow>> GetLatestTerminalInteractionsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var latestInteractionIds = _context.ProposalInteractionEvents
            .AsNoTracking()
            .Where(interaction =>
                interaction.UserId == userId &&
                TerminalActions.Contains(interaction.InteractionType))
            .GroupBy(interaction => interaction.ProjectLawId)
            .Select(group => group
                .OrderByDescending(interaction => interaction.CreatedAtUtc)
                .ThenByDescending(interaction => interaction.Id)
                .Select(interaction => interaction.Id)
                .First());

        return await _context.ProposalInteractionEvents
            .AsNoTracking()
            .Where(interaction => latestInteractionIds.Contains(interaction.Id))
            .Select(interaction => new LatestInteractionRow(
                interaction.ProjectLawId,
                interaction.InteractionType))
            .ToListAsync(cancellationToken);
    }

    private static ProfileOverviewResponse BuildOverview(
        IReadOnlyCollection<LatestInteractionRow> latestInteractions)
    {
        var proposalsInteracted = latestInteractions.Count;
        var supportCount = latestInteractions.Count(item => item.InteractionType == ProposalInteractionType.Support);
        var opposeCount = latestInteractions.Count(item => item.InteractionType == ProposalInteractionType.Oppose);
        var abstentionCount = latestInteractions.Count(item => item.InteractionType == ProposalInteractionType.Abstain);
        var skipCount = latestInteractions.Count(item => item.InteractionType == ProposalInteractionType.Skip);

        return new ProfileOverviewResponse
        {
            ProposalsInteracted = proposalsInteracted,
            SupportCount = supportCount,
            OpposeCount = opposeCount,
            AbstentionCount = abstentionCount,
            SkipCount = skipCount,
            SupportRate = Percentage(supportCount, proposalsInteracted),
            SkipRate = Percentage(skipCount, proposalsInteracted)
        };
    }

    private async Task<PartyAlignmentSectionResponse> BuildPartyAlignmentAsync(
        IReadOnlyCollection<LatestInteractionRow> latestInteractions,
        CancellationToken cancellationToken)
    {
        var comparableUserVotes = latestInteractions
            .Where(item => UserComparableActions.Contains(item.InteractionType))
            .GroupBy(item => item.ProjectLawId)
            .ToDictionary(
                group => group.Key,
                group => ToPartyOrientation(group.First().InteractionType));
        var comparableProjectLawIds = comparableUserVotes.Keys.ToList();

        List<AlignmentVoteRow> alignmentRows = comparableUserVotes.Count == 0
            ? []
            : await _context.ParliamentInitiativeVoteBlocks
                .AsNoTracking()
                .Where(block =>
                    block.ParliamentInitiativeVote != null &&
                    block.ParliamentInitiativeVote.Stage == GeneralityStage &&
                    comparableProjectLawIds.Contains(block.ParliamentInitiativeVote.ProjectLawId) &&
                    block.PartyAcronym != null &&
                    block.PartyAcronym != string.Empty &&
                    block.IsUnanimousWithinParty != false &&
                    PartyComparableOrientations.Contains(block.VotingOrientation))
                .Select(block => new AlignmentVoteRow(
                    block.ParliamentInitiativeVote!.ProjectLawId,
                    block.PartyAcronym!,
                    block.VotingOrientation))
                .ToListAsync(cancellationToken);

        var totalComparableVotes = alignmentRows
            .Select(row => row.ProjectLawId)
            .Distinct()
            .Count();

        var knownParties = await _context.PoliticalParties
            .AsNoTracking()
            .Where(party => party.partyAcronym != GovernmentAcronym)
            .Select(party => new PartyMetadataRow(
                party.partyAcronym ?? string.Empty,
                party.fullName ?? string.Empty,
                party.logoLink))
            .ToListAsync(cancellationToken);

        var partyAcronyms = alignmentRows
            .Select(row => row.PartyAcronym)
            .Where(acronym => !string.IsNullOrWhiteSpace(acronym))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var knownPartyByAcronym = knownParties
            .GroupBy(party => party.Acronym, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var statsByParty = alignmentRows
            .GroupBy(row => row.PartyAcronym, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var comparableCount = 0;
                    var alignedCount = 0;

                    foreach (var row in group)
                    {
                        if (!comparableUserVotes.TryGetValue(row.ProjectLawId, out var userOrientation))
                        {
                            continue;
                        }

                        comparableCount += 1;
                        if (userOrientation == row.PartyOrientation)
                        {
                            alignedCount += 1;
                        }
                    }

                    return new PartyAlignmentCounts(alignedCount, comparableCount);
                },
                StringComparer.OrdinalIgnoreCase);

        var parties = partyAcronyms
            .Select(acronym =>
            {
                statsByParty.TryGetValue(acronym, out var stats);
                knownPartyByAcronym.TryGetValue(acronym, out var metadata);

                var alignedCount = stats?.AlignedCount ?? 0;
                var comparableCount = stats?.ComparableCount ?? 0;

                return new PartyAlignmentResponse
                {
                    PartyId = acronym,
                    PartyAcronym = acronym,
                    PartyName = string.IsNullOrWhiteSpace(metadata?.Name) ? acronym : metadata.Name,
                    PartyLogo = metadata?.Logo,
                    AlignedCount = alignedCount,
                    ComparableCount = comparableCount,
                    AlignmentPercentage = Percentage(alignedCount, comparableCount)
                };
            })
            .Where(party => party.ComparableCount > 0)
            .OrderByDescending(party => party.AlignmentPercentage)
            .ThenByDescending(party => party.ComparableCount)
            .ThenBy(party => party.PartyAcronym)
            .ToList();

        return new PartyAlignmentSectionResponse
        {
            IsUnlocked = totalComparableVotes >= MinimumComparableVotes,
            MinimumComparableVotes = MinimumComparableVotes,
            TotalComparableVotes = totalComparableVotes,
            Parties = parties
        };
    }

    private static VotingOrientation ToPartyOrientation(ProposalInteractionType interactionType)
    {
        return interactionType switch
        {
            ProposalInteractionType.Support => VotingOrientation.InFavor,
            ProposalInteractionType.Oppose => VotingOrientation.Against,
            ProposalInteractionType.Abstain => VotingOrientation.Abstaining,
            _ => VotingOrientation.NotInterested
        };
    }

    private static decimal Percentage(int numerator, int denominator)
    {
        return denominator == 0
            ? 0
            : Math.Round(numerator * 100m / denominator, 1);
    }

    private sealed record LatestInteractionRow(
        int ProjectLawId,
        ProposalInteractionType InteractionType);

    private sealed record AlignmentVoteRow(
        int ProjectLawId,
        string PartyAcronym,
        VotingOrientation PartyOrientation);

    private sealed record PartyMetadataRow(
        string Acronym,
        string Name,
        string? Logo);

    private sealed record PartyAlignmentCounts(
        int AlignedCount,
        int ComparableCount);
}
