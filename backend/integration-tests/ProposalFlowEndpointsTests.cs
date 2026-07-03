using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Parlamento.Domain.Entities;
using Parlamento.Domain.Enums;
using Parlamento.Infrastructure.Persistence;

using Xunit;

namespace integration_tests;

public sealed class ProposalFlowEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    public int EligibleInitiativeId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var ps = db.PoliticalParties.Single(party => party.partyAcronym == "PS");
        var psd = db.PoliticalParties.Single(party => party.partyAcronym == "PSD");

        var user = new User
        {
            ProfilePic = 1,
            UserName = "proposal-flow-tester",
            Email = "proposal-flow-tester@example.com",
            Password = "hashed-password",
            Votes = new List<Vote>()
        };

        var eligibleInitiative = new ProjectLaw
        {
            SourceId = 2001,
            Legislatura = "XV",
            InitiativeNumber = "10/XV/1",
            InitiativeTypeDescription = "Projeto de Lei",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-02-01",
            ProposingParty = ps,
            ProposalTitle = "Original title that may be less neutral",
            FullProposalTextLink = "https://example.com/proposals/2001",
            ProposalResult = ProposalResult.ApprovedInGenerality,
            VotingResultGenerality = new VotingResult
            {
                isUninamous = false,
                votingBlocks = new List<VotingBlock>
                {
                    new()
                    {
                        politicalPartyAcronym = "PS",
                        votingOrientation = VotingOrientation.InFavor
                    }
                }
            },
            ImportedDocuments = new List<ParliamentInitiativeDocument>
            {
                new()
                {
                    Scope = "Initiative",
                    Name = "Introduced text",
                    Content = new ParliamentDocumentContent
                    {
                        RedactionStatus = "Succeeded",
                        ExtractionStatus = "Succeeded",
                        RedactedContentText = "Texto anonimo da iniciativa com identidade ocultada."
                    }
                }
            },
            Summaries = new List<ParliamentSummary>
            {
                new()
                {
                    GenerationStatus = "Succeeded",
                    ModelName = "test-model",
                    PromptVersion = "test-prompt",
                    SourceDocumentHash = "hash",
                    ShortTitle = "Titulo neutro gerado",
                    SummaryText = "Resumo seguro para mostrar antes do voto.",
                    GeneratedAtUtc = DateTime.UtcNow
                }
            }
        };

        var excludedInitiative = new ProjectLaw
        {
            SourceId = 2002,
            Legislatura = "XV",
            InitiativeTypeDescription = "Projeto de Lei",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-02-02",
            ProposingParty = psd,
            ProposalTitle = "Already voted title",
            FullProposalTextLink = "https://example.com/proposals/2002",
            ProposalResult = ProposalResult.RejectedInGenerality
        };

        db.Users.Add(user);
        db.ProjectLaws.AddRange(eligibleInitiative, excludedInitiative);
        db.SaveChanges();

        user.Votes.Add(new Vote
        {
            ProjectLawID = excludedInitiative.Id,
            VoteDate = DateTime.UtcNow,
            VotingOrientation = VotingOrientation.Against
        });
        db.SaveChanges();

        UserId = user.Id;
        EligibleInitiativeId = eligibleInitiative.Id;
    }
}

public sealed class ProposalFlowEndpointsTests : IClassFixture<ProposalFlowEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly ProposalFlowEndpointsFactory _factory;

    public ProposalFlowEndpointsTests(ProposalFlowEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task FeedReturnsOnlyPreVoteSafeInitiativeFields()
    {
        var response = await _client.PostAsJsonAsync("/proposal-flow/feed", new
        {
            userId = _factory.UserId,
            legislatures = new[] { "XV" }
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(_factory.EligibleInitiativeId, root.GetProperty("initiativeId").GetInt32());
        Assert.Equal("Projeto de Lei", root.GetProperty("initiativeType").GetString());
        Assert.Equal("Titulo neutro gerado", root.GetProperty("neutralTitle").GetString());
        Assert.Equal("Resumo seguro para mostrar antes do voto.", root.GetProperty("summary").GetString());
        Assert.Contains("Texto anonimo", root.GetProperty("redactedExcerpt").GetString());

        Assert.DoesNotContain("proposingParty", body);
        Assert.DoesNotContain("proposalResult", body);
        Assert.DoesNotContain("votingResult", body);
        Assert.DoesNotContain("PS", body);
    }
}

public sealed class ProposalInteractionEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    public int SupportInitiativeId { get; private set; }

    public int SkipInitiativeId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var ps = db.PoliticalParties.Single(party => party.partyAcronym == "PS");

        var user = new User
        {
            ProfilePic = 1,
            UserName = "proposal-interaction-tester",
            Email = "proposal-interaction-tester@example.com",
            Password = "hashed-password"
        };

        var supportInitiative = CreateInitiative(3001, "Support candidate", ps);
        var skipInitiative = CreateInitiative(3002, "Skip candidate", ps);

        db.Users.Add(user);
        db.ProjectLaws.AddRange(supportInitiative, skipInitiative);
        db.SaveChanges();

        UserId = user.Id;
        SupportInitiativeId = supportInitiative.Id;
        SkipInitiativeId = skipInitiative.Id;
    }

    private static ProjectLaw CreateInitiative(int sourceId, string title, PoliticalParty proposingParty)
    {
        return new ProjectLaw
        {
            SourceId = sourceId,
            Legislatura = "XV",
            InitiativeTypeDescription = "Projeto de Lei",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-03-01",
            ProposingParty = proposingParty,
            ProposalTitle = title,
            FullProposalTextLink = $"https://example.com/proposals/{sourceId}",
            ProposalResult = ProposalResult.ApprovedInGenerality
        };
    }
}

public sealed class ProposalInteractionEndpointsTests : IClassFixture<ProposalInteractionEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly ProposalInteractionEndpointsFactory _factory;

    public ProposalInteractionEndpointsTests(ProposalInteractionEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task InteractionEndpointRecordsActionsIdempotentlyAndKeepsSkipSeparateFromAbstention()
    {
        var supportResponse = await _client.PostAsJsonAsync("/proposal-flow/interactions", new
        {
            userId = _factory.UserId,
            initiativeId = _factory.SupportInitiativeId,
            action = "Support",
            idempotencyKey = "support-submit-1"
        });
        supportResponse.EnsureSuccessStatusCode();

        using var supportDocument = JsonDocument.Parse(await supportResponse.Content.ReadAsStringAsync());
        var supportRoot = supportDocument.RootElement;
        var interactionId = supportRoot.GetProperty("interactionId").GetInt32();
        Assert.False(supportRoot.GetProperty("isDuplicate").GetBoolean());
        Assert.Equal("Support", supportRoot.GetProperty("action").GetString());

        var duplicateResponse = await _client.PostAsJsonAsync("/proposal-flow/interactions", new
        {
            userId = _factory.UserId,
            initiativeId = _factory.SupportInitiativeId,
            action = "Support",
            idempotencyKey = "support-submit-1"
        });
        duplicateResponse.EnsureSuccessStatusCode();

        using var duplicateDocument = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());
        var duplicateRoot = duplicateDocument.RootElement;
        Assert.True(duplicateRoot.GetProperty("isDuplicate").GetBoolean());
        Assert.Equal(interactionId, duplicateRoot.GetProperty("interactionId").GetInt32());

        var skipResponse = await _client.PostAsJsonAsync("/proposal-flow/interactions", new
        {
            userId = _factory.UserId,
            initiativeId = _factory.SkipInitiativeId,
            action = "Skip"
        });
        skipResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var events = db.ProposalInteractionEvents.ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, item => item.InteractionType == ProposalInteractionType.Support);
        Assert.Contains(events, item => item.InteractionType == ProposalInteractionType.Skip);

        var supportStats = db.ProjectLawInteractionStats.Single(item =>
            item.ProjectLawId == _factory.SupportInitiativeId);
        Assert.Equal(1, supportStats.SupportVotes);
        Assert.Equal(0, supportStats.Skips);

        var skipStats = db.ProjectLawInteractionStats.Single(item =>
            item.ProjectLawId == _factory.SkipInitiativeId);
        Assert.Equal(0, skipStats.SupportVotes);
        Assert.Equal(1, skipStats.Skips);

        var userVotes = db.Users
            .Where(user => user.Id == _factory.UserId)
            .SelectMany(user => user.Votes)
            .ToList();
        Assert.Empty(userVotes);
    }
}
