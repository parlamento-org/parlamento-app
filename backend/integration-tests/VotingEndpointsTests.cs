using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

using Xunit;

namespace integration_tests;

public class VotingEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    public int AvailableProposalId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var ps = db.PoliticalParties!.Single(p => p.partyAcronym == "PS");
        var psd = db.PoliticalParties.Single(p => p.partyAcronym == "PSD");

        var user = new User
        {
            ProfilePic = 1,
            UserName = "vote-tester",
            Email = "vote-tester@example.com",
            Password = "hashed-password",
            Votes = new List<Vote>
            {
                new Vote
                {
                    ProjectLawID = 999999,
                    VoteDate = DateTime.UtcNow.AddDays(-1),
                    VotingOrientation = VotingOrientation.Against
                }
            }
        };

        var availableProposal = new ProjectLaw
        {
            SourceId = 1001,
            Legislatura = "XV",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-01-10",
            ProposingParty = ps,
            ProposalTitle = "Available proposal",
            FullProposalTextLink = "https://example.com/proposals/1001",
            ProposalTextHTML = "<p>Proposal text</p>",
            ProposalResult = ProposalResult.ApprovedInGenerality,
            VotingResultGenerality = new VotingResult
            {
                isUninamous = false,
                votingBlocks = new List<VotingBlock>
                {
                    new VotingBlock
                    {
                        politicalPartyAcronym = "PS",
                        votingOrientation = VotingOrientation.InFavor
                    },
                    new VotingBlock
                    {
                        politicalPartyAcronym = "PSD",
                        votingOrientation = VotingOrientation.Against
                    }
                }
            }
        };

        var excludedProposal = new ProjectLaw
        {
            SourceId = 1002,
            Legislatura = "XV",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-01-11",
            ProposingParty = psd,
            ProposalTitle = "Previously voted proposal",
            FullProposalTextLink = "https://example.com/proposals/1002",
            ProposalTextHTML = "<p>Previously voted text</p>",
            ProposalResult = ProposalResult.RejectedInGenerality,
            VotingResultGenerality = new VotingResult
            {
                isUninamous = false,
                votingBlocks = new List<VotingBlock>
                {
                    new VotingBlock
                    {
                        politicalPartyAcronym = "PS",
                        votingOrientation = VotingOrientation.Against
                    }
                }
            }
        };

        db.Users!.Add(user);
        db.ProjectLaws!.AddRange(availableProposal, excludedProposal);
        db.SaveChanges();

        user.Votes[0].ProjectLawID = excludedProposal.Id;
        db.SaveChanges();

        UserId = user.Id;
        AvailableProposalId = availableProposal.Id;
    }
}

public class VotingEndpointsTests : IClassFixture<VotingEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly VotingEndpointsFactory _factory;

    public VotingEndpointsTests(VotingEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task ProposalFeedReturnsAvailableProposalForCurrentUser()
    {
        var response = await _client.PutAsJsonAsync("/vote", new
        {
            userID = _factory.UserId,
            lowestScoreAllowed = 0,
            legislaturas = new[] { "XV" }
        });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(_factory.AvailableProposalId, root.GetProperty("id").GetInt32());
        Assert.Equal("Available proposal", root.GetProperty("proposalTitle").GetString());
    }

    [Fact]
    public async Task VoteEndpointAppendsVoteForProposal()
    {
        var response = await _client.PostAsJsonAsync("/vote", new
        {
            userID = _factory.UserId,
            projectLawID = _factory.AvailableProposalId,
            votingOrientation = "InFavor"
        });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var votes = document.RootElement.GetProperty("votes").EnumerateArray().ToList();

        Assert.Equal(2, votes.Count);
        Assert.Contains(votes, vote => vote.GetProperty("projectLawID").GetInt32() == _factory.AvailableProposalId);
    }
}
