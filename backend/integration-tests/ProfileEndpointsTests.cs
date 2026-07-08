using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

using Xunit;

namespace integration_tests;

public sealed class ProfileEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    public int LockedUserId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var ps = db.PoliticalParties.Single(party => party.partyAcronym == "PS");
        var psd = db.PoliticalParties.Single(party => party.partyAcronym == "PSD");

        var user = new User
        {
            ProfilePic = 1,
            UserName = "profile-tester",
            Email = "profile-tester@example.com",
            Password = "hashed-password"
        };
        var lockedUser = new User
        {
            ProfilePic = 2,
            UserName = "profile-locked",
            Email = "profile-locked@example.com",
            Password = "hashed-password"
        };

        db.Users.AddRange(user, lockedUser);

        var actions = new[]
        {
            ProposalInteractionType.Support,
            ProposalInteractionType.Support,
            ProposalInteractionType.Support,
            ProposalInteractionType.Support,
            ProposalInteractionType.Oppose,
            ProposalInteractionType.Oppose,
            ProposalInteractionType.Oppose,
            ProposalInteractionType.Abstain,
            ProposalInteractionType.Abstain,
            ProposalInteractionType.Support,
            ProposalInteractionType.Oppose
        };

        var initiatives = actions
            .Select((action, index) => CreateInitiative(
                sourceId: 7000 + index,
                title: $"Profile initiative {index}",
                proposingParty: index % 2 == 0 ? ps : psd,
                psOrientation: ToPartyOrientation(action),
                psdOrientation: index == 0 ? VotingOrientation.Absent : OppositeOrientation(action)))
            .ToList();

        var skippedInitiative = CreateInitiative(
            8000,
            "Skipped profile initiative",
            ps,
            VotingOrientation.InFavor,
            VotingOrientation.Against);

        db.ProjectLaws.AddRange(initiatives);
        db.ProjectLaws.Add(skippedInitiative);
        db.SaveChanges();

        var now = DateTime.UtcNow;
        for (var i = 0; i < initiatives.Count; i++)
        {
            db.ProposalInteractionEvents.Add(new ProposalInteractionEvent
            {
                UserId = user.Id,
                ProjectLawId = initiatives[i].Id,
                InteractionType = actions[i],
                CreatedAtUtc = now.AddMinutes(i)
            });
        }

        db.ProposalInteractionEvents.Add(new ProposalInteractionEvent
        {
            UserId = user.Id,
            ProjectLawId = skippedInitiative.Id,
            InteractionType = ProposalInteractionType.Skip,
            CreatedAtUtc = now.AddMinutes(20)
        });

        foreach (var initiative in initiatives.Take(3))
        {
            db.ProposalInteractionEvents.Add(new ProposalInteractionEvent
            {
                UserId = lockedUser.Id,
                ProjectLawId = initiative.Id,
                InteractionType = ProposalInteractionType.Support,
                CreatedAtUtc = now
            });
        }

        db.SaveChanges();

        UserId = user.Id;
        LockedUserId = lockedUser.Id;
    }

    private static ProjectLaw CreateInitiative(
        int sourceId,
        string title,
        PoliticalParty proposingParty,
        VotingOrientation psOrientation,
        VotingOrientation psdOrientation)
    {
        return new ProjectLaw
        {
            SourceId = sourceId,
            Legislatura = "XV",
            InitiativeTypeDescription = "Projeto de Lei",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-06-01",
            ProposingParty = proposingParty,
            ProposalTitle = title,
            FullProposalTextLink = $"https://example.com/proposals/{sourceId}",
            ProposalResult = ProposalResult.ApprovedInGenerality,
            ImportedVotes = new List<ParliamentInitiativeVote>
            {
                new()
                {
                    Stage = "Generality",
                    VoteDate = "2024-06-02",
                    Blocks = new List<ParliamentInitiativeVoteBlock>
                    {
                        new()
                        {
                            PartyAcronym = "PS",
                            VotingOrientation = psOrientation,
                            IsUnanimousWithinParty = true
                        },
                        new()
                        {
                            PartyAcronym = "PSD",
                            VotingOrientation = psdOrientation,
                            IsUnanimousWithinParty = true
                        }
                    }
                }
            }
        };
    }

    private static VotingOrientation ToPartyOrientation(ProposalInteractionType action)
    {
        return action switch
        {
            ProposalInteractionType.Support => VotingOrientation.InFavor,
            ProposalInteractionType.Oppose => VotingOrientation.Against,
            ProposalInteractionType.Abstain => VotingOrientation.Abstaining,
            _ => VotingOrientation.NotInterested
        };
    }

    private static VotingOrientation OppositeOrientation(ProposalInteractionType action)
    {
        return action switch
        {
            ProposalInteractionType.Support => VotingOrientation.Against,
            ProposalInteractionType.Oppose => VotingOrientation.InFavor,
            ProposalInteractionType.Abstain => VotingOrientation.InFavor,
            _ => VotingOrientation.NotInterested
        };
    }
}

public sealed class ProfileEndpointsTests : IClassFixture<ProfileEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly ProfileEndpointsFactory _factory;

    public ProfileEndpointsTests(ProfileEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _client.AuthenticateAsUser(_factory.UserId);
    }

    [Fact]
    public async Task ProfileEndpointReturnsOverviewAndUnlockedAlignment()
    {
        var response = await _client.GetAsync("/profile");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var overview = root.GetProperty("overview");

        Assert.Equal(12, overview.GetProperty("proposalsInteracted").GetInt32());
        Assert.Equal(5, overview.GetProperty("supportCount").GetInt32());
        Assert.Equal(4, overview.GetProperty("opposeCount").GetInt32());
        Assert.Equal(2, overview.GetProperty("abstentionCount").GetInt32());
        Assert.Equal(1, overview.GetProperty("skipCount").GetInt32());

        var partyAlignment = root.GetProperty("partyAlignment");
        Assert.True(partyAlignment.GetProperty("isUnlocked").GetBoolean());
        Assert.Equal(11, partyAlignment.GetProperty("totalComparableVotes").GetInt32());

        var parties = partyAlignment.GetProperty("parties").EnumerateArray().ToList();
        var ps = parties.Single(party => party.GetProperty("partyAcronym").GetString() == "PS");
        Assert.Equal(11, ps.GetProperty("alignedCount").GetInt32());
        Assert.Equal(11, ps.GetProperty("comparableCount").GetInt32());
        Assert.Equal(100, ps.GetProperty("alignmentPercentage").GetDecimal());

        var psd = parties.Single(party => party.GetProperty("partyAcronym").GetString() == "PSD");
        Assert.Equal(0, psd.GetProperty("alignedCount").GetInt32());
        Assert.Equal(10, psd.GetProperty("comparableCount").GetInt32());
    }

    [Fact]
    public async Task ProfileEndpointLocksAlignmentBelowComparableVoteThreshold()
    {
        _client.AuthenticateAsUser(_factory.LockedUserId);

        var response = await _client.GetAsync("/profile");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var partyAlignment = document.RootElement.GetProperty("partyAlignment");

        Assert.False(partyAlignment.GetProperty("isUnlocked").GetBoolean());
        Assert.Equal(10, partyAlignment.GetProperty("minimumComparableVotes").GetInt32());
        Assert.Equal(3, partyAlignment.GetProperty("totalComparableVotes").GetInt32());
    }
}
