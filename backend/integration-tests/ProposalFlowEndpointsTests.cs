using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

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
