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
        _client.AuthenticateAsUser(_factory.UserId);
    }

    [Fact]
    public async Task FeedReturnsOnlyPreVoteSafeInitiativeFields()
    {
        var response = await _client.PostAsJsonAsync("/proposal-flow/feed", new
        {
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
        _client.AuthenticateAsUser(_factory.UserId);
    }

    [Fact]
    public async Task InteractionEndpointRecordsActionsIdempotentlyAndKeepsSkipSeparateFromAbstention()
    {
        var supportResponse = await _client.PostAsJsonAsync("/proposal-flow/interactions", new
        {
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

public sealed class ProposalHistoryEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    public int EmptyUserId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var ps = db.PoliticalParties.Single(party => party.partyAcronym == "PS");
        var psd = db.PoliticalParties.Single(party => party.partyAcronym == "PSD");

        var user = new User
        {
            ProfilePic = 1,
            UserName = "proposal-history-tester",
            Email = "proposal-history-tester@example.com",
            Password = "hashed-password"
        };
        var emptyUser = new User
        {
            ProfilePic = 2,
            UserName = "proposal-history-empty",
            Email = "proposal-history-empty@example.com",
            Password = "hashed-password"
        };

        var xvOld = CreateInitiative(5001, "Older XV initiative", "XV", ps);
        var xvDuplicate = CreateInitiative(5002, "Latest duplicate XV initiative", "XV", psd);
        var xvi = CreateInitiative(5003, "Only XVI initiative", "XVI", ps);
        var xvii = CreateInitiative(5004, "Newest XVII initiative", "XVII", psd);

        db.Users.AddRange(user, emptyUser);
        db.ProjectLaws.AddRange(xvOld, xvDuplicate, xvi, xvii);
        db.SaveChanges();

        var now = DateTime.UtcNow;
        db.ProposalInteractionEvents.AddRange(
            CreateInteraction(user.Id, xvOld.Id, ProposalInteractionType.Support, now.AddMinutes(-40)),
            CreateInteraction(user.Id, xvDuplicate.Id, ProposalInteractionType.Oppose, now.AddMinutes(-30)),
            CreateInteraction(user.Id, xvDuplicate.Id, ProposalInteractionType.Support, now.AddMinutes(-10)),
            CreateInteraction(user.Id, xvi.Id, ProposalInteractionType.Skip, now.AddMinutes(-20)),
            CreateInteraction(user.Id, xvii.Id, ProposalInteractionType.Abstain, now.AddMinutes(-5)));
        db.SaveChanges();

        UserId = user.Id;
        EmptyUserId = emptyUser.Id;
    }

    private static ProjectLaw CreateInitiative(
        int sourceId,
        string title,
        string legislature,
        PoliticalParty proposingParty)
    {
        return new ProjectLaw
        {
            SourceId = sourceId,
            Legislatura = legislature,
            InitiativeNumber = $"{sourceId}/{legislature}/1",
            InitiativeTypeDescription = "Projeto de Lei",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-05-01",
            ProposingParty = proposingParty,
            ProposalTitle = title,
            FullProposalTextLink = $"https://example.com/proposals/{sourceId}",
            ProposalResult = ProposalResult.ApprovedInGenerality
        };
    }

    private static ProposalInteractionEvent CreateInteraction(
        int userId,
        int initiativeId,
        ProposalInteractionType action,
        DateTime createdAtUtc)
    {
        return new ProposalInteractionEvent
        {
            UserId = userId,
            ProjectLawId = initiativeId,
            InteractionType = action,
            CreatedAtUtc = createdAtUtc
        };
    }
}

public sealed class ProposalHistoryEndpointsTests : IClassFixture<ProposalHistoryEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly ProposalHistoryEndpointsFactory _factory;

    public ProposalHistoryEndpointsTests(ProposalHistoryEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _client.AuthenticateAsUser(_factory.UserId);
    }

    [Fact]
    public async Task HistoryEndpointReturnsPaginatedMetadata()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=2");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var items = root.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(2, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(4, root.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
        Assert.True(root.GetProperty("hasNextPage").GetBoolean());
        Assert.False(root.GetProperty("hasPreviousPage").GetBoolean());
        Assert.Equal("Newest XVII initiative", items[0].GetProperty("title").GetString());
        Assert.Equal("Latest duplicate XV initiative", items[1].GetProperty("title").GetString());
        Assert.Equal("Support", items[1].GetProperty("action").GetString());
        Assert.Contains(
            root.GetProperty("availableLegislatures").EnumerateArray(),
            item => item.GetString() == "XVI");
        Assert.Contains(
            root.GetProperty("availableProposingParties").EnumerateArray(),
            item => item.GetProperty("acronym").GetString() == "PS");
    }

    [Fact]
    public async Task HistoryEndpointFiltersByLegislature()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=10&legislature=XVI");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var item = root.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(1, root.GetProperty("totalItems").GetInt32());
        Assert.Equal("XVI", item.GetProperty("legislature").GetString());
        Assert.Equal("Only XVI initiative", item.GetProperty("title").GetString());
        Assert.False(root.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task HistoryEndpointFiltersByProposingParty()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=10&proposingParty=PSD");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var items = root.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(2, root.GetProperty("totalItems").GetInt32());
        Assert.All(items, item =>
            Assert.Contains(
                item.GetProperty("proposers").EnumerateArray(),
                proposer => proposer.GetProperty("acronym").GetString() == "PSD"));
    }

    [Fact]
    public async Task HistoryEndpointCombinesLegislatureAndProposingPartyFilters()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=10&legislature=XV&proposingParty=PSD");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var item = root.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(1, root.GetProperty("totalItems").GetInt32());
        Assert.Equal("XV", item.GetProperty("legislature").GetString());
        Assert.Equal("Latest duplicate XV initiative", item.GetProperty("title").GetString());
        Assert.Contains(
            item.GetProperty("proposers").EnumerateArray(),
            proposer => proposer.GetProperty("acronym").GetString() == "PSD");
    }

    [Fact]
    public async Task HistoryEndpointPaginatesWithFiltersApplied()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=1&proposingParty=PS");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Single(root.GetProperty("items").EnumerateArray());
        Assert.Equal(2, root.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
        Assert.True(root.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task HistoryEndpointSearchesByTitle()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=10&search=Older");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var item = root.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(1, root.GetProperty("totalItems").GetInt32());
        Assert.Equal("Older XV initiative", item.GetProperty("title").GetString());
    }

    [Fact]
    public async Task HistoryEndpointCombinesSearchAndLegislatureFilter()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=10&search=initiative&legislature=XVII");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var item = root.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(1, root.GetProperty("totalItems").GetInt32());
        Assert.Equal("XVII", item.GetProperty("legislature").GetString());
        Assert.Equal("Newest XVII initiative", item.GetProperty("title").GetString());
    }

    [Fact]
    public async Task HistoryEndpointCombinesSearchAndProposingPartyFilter()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=10&search=Newest&proposingParty=PSD");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var item = root.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(1, root.GetProperty("totalItems").GetInt32());
        Assert.Equal("Newest XVII initiative", item.GetProperty("title").GetString());
        Assert.Contains(
            item.GetProperty("proposers").EnumerateArray(),
            proposer => proposer.GetProperty("acronym").GetString() == "PSD");
    }

    [Fact]
    public async Task HistoryEndpointReturnsPaginationMetadataUnderSearch()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=2&search=initiative");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("items").EnumerateArray().Count());
        Assert.Equal(4, root.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
        Assert.True(root.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task HistoryEndpointReturnsEmptySearchResults()
    {
        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=20&search=zzzz-not-found");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Empty(root.GetProperty("items").EnumerateArray());
        Assert.Equal(0, root.GetProperty("totalItems").GetInt32());
        Assert.Equal(0, root.GetProperty("totalPages").GetInt32());
        Assert.False(root.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task HistoryEndpointReturnsEmptyResults()
    {
        _client.AuthenticateAsUser(_factory.EmptyUserId);

        var response = await _client.GetAsync("/proposal-flow/history?page=1&pageSize=20");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Empty(root.GetProperty("items").EnumerateArray());
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(20, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(0, root.GetProperty("totalItems").GetInt32());
        Assert.Equal(0, root.GetProperty("totalPages").GetInt32());
        Assert.False(root.GetProperty("hasNextPage").GetBoolean());
        Assert.False(root.GetProperty("hasPreviousPage").GetBoolean());
    }
}

public sealed class ProposalRevealEndpointsFactory : TestingWebAppFactory
{
    public int UserId { get; private set; }

    public int OtherUserId { get; private set; }

    public int InitiativeId { get; private set; }

    protected override void SeedDbForTests(DatabaseContext db)
    {
        var ps = db.PoliticalParties.Single(party => party.partyAcronym == "PS");

        var user = new User
        {
            ProfilePic = 1,
            UserName = "proposal-reveal-tester",
            Email = "proposal-reveal-tester@example.com",
            Password = "hashed-password"
        };
        var otherUser = new User
        {
            ProfilePic = 2,
            UserName = "proposal-reveal-locked",
            Email = "proposal-reveal-locked@example.com",
            Password = "hashed-password"
        };

        var initiative = new ProjectLaw
        {
            SourceId = 4001,
            Legislatura = "XV",
            InitiativeNumber = "40/XV/1",
            InitiativeTypeDescription = "Projeto de Lei",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2024-04-01",
            ProposingParty = ps,
            ProposalTitle = "Reveal candidate",
            FullProposalTextLink = "https://example.com/proposals/4001",
            ProposalResult = ProposalResult.ApprovedInGenerality,
            ImportedAuthors = new List<ParliamentInitiativeAuthor>
            {
                new()
                {
                    AuthorKind = "ParliamentaryGroup",
                    Acronym = "PS",
                    Name = "Partido Socialista"
                }
            },
            ImportedDocuments = new List<ParliamentInitiativeDocument>
            {
                new()
                {
                    Scope = "InitiativeText",
                    Name = "Texto da iniciativa",
                    Url = "https://example.com/proposals/4001/text"
                }
            },
            ImportedPublications = new List<ParliamentInitiativePublication>
            {
                new()
                {
                    Scope = "EventPublication",
                    Type = "Diario",
                    DiaryUrl = "https://example.com/diario/4001"
                }
            },
            ImportedVotes = new List<ParliamentInitiativeVote>
            {
                new()
                {
                    Stage = "Generality",
                    VoteDate = "2024-04-02",
                    Description = "Votacao na generalidade",
                    Result = "Aprovado",
                    Blocks = new List<ParliamentInitiativeVoteBlock>
                    {
                        new()
                        {
                            PartyAcronym = "PS",
                            VotingOrientation = VotingOrientation.InFavor,
                            NumberOfDeputies = 120,
                            IsUnanimousWithinParty = true
                        },
                        new()
                        {
                            PartyAcronym = "PSD",
                            VotingOrientation = VotingOrientation.Against,
                            NumberOfDeputies = 80,
                            IsUnanimousWithinParty = true
                        }
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
                    ShortTitle = "Titulo neutro reveal",
                    SummaryText = "Resumo do reveal.",
                    GeneratedAtUtc = DateTime.UtcNow
                }
            }
        };

        db.Users.AddRange(user, otherUser);
        db.ProjectLaws.Add(initiative);
        db.SaveChanges();

        db.ProposalInteractionEvents.Add(new ProposalInteractionEvent
        {
            UserId = user.Id,
            ProjectLawId = initiative.Id,
            InteractionType = ProposalInteractionType.Support,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();

        UserId = user.Id;
        OtherUserId = otherUser.Id;
        InitiativeId = initiative.Id;
    }
}

public sealed class ProposalRevealEndpointsTests : IClassFixture<ProposalRevealEndpointsFactory>
{
    private readonly HttpClient _client;
    private readonly ProposalRevealEndpointsFactory _factory;

    public ProposalRevealEndpointsTests(ProposalRevealEndpointsFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _client.AuthenticateAsUser(_factory.UserId);
    }

    [Fact]
    public async Task RevealEndpointReturnsPostVoteProposerOutcomeSourcesAndJourneyAction()
    {
        var response = await _client.GetAsync(
            $"/proposal-flow/initiatives/{_factory.InitiativeId}/reveal");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(_factory.InitiativeId, root.GetProperty("initiativeId").GetInt32());
        Assert.Equal("Support", root.GetProperty("userVote").GetString());
        Assert.Equal("Projeto de Lei", root.GetProperty("initiativeType").GetString());
        Assert.Equal("Titulo neutro reveal", root.GetProperty("title").GetString());

        var proposer = root.GetProperty("proposers").EnumerateArray().Single();
        Assert.Equal("ParliamentaryGroup", proposer.GetProperty("kind").GetString());
        Assert.Equal("PS", proposer.GetProperty("acronym").GetString());

        var generalityVote = root.GetProperty("generalityVote");
        Assert.Equal("250", generalityVote.GetProperty("stageCode").GetString());
        Assert.Equal("Aprovado", generalityVote.GetProperty("result").GetString());
        Assert.True(generalityVote.GetProperty("approved").GetBoolean());
        Assert.Contains(
            generalityVote.GetProperty("partyVotes").EnumerateArray(),
            item => item.GetProperty("partyAcronym").GetString() == "PSD" &&
                    item.GetProperty("orientation").GetString() == "Against");

        Assert.Contains(
            root.GetProperty("officialSources").EnumerateArray(),
            item => item.GetProperty("url").GetString() == "https://example.com/diario/4001");
        Assert.Equal(
            $"/proposal-flow/initiatives/{_factory.InitiativeId}/journey",
            root.GetProperty("journey").GetProperty("endpoint").GetString());
    }

    [Fact]
    public async Task RevealEndpointRequiresUserVoteInteraction()
    {
        _client.AuthenticateAsUser(_factory.OtherUserId);

        var response = await _client.GetAsync(
            $"/proposal-flow/initiatives/{_factory.InitiativeId}/reveal");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }
}
