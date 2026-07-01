using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Parlamento.Domain.Enums;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services.ParliamentOpenData;

using Xunit;

namespace integration_tests;

public class ParliamentOpenDataImportServiceTests
{
    [Fact]
    public async Task ImportFromFileAsync_ImportsSingleObjectSampleAndIsIdempotent()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);
        var samplePath = SamplePath("example_iniciativa.json");

        var firstRun = await service.ImportFromFileAsync(samplePath);
        var secondRun = await service.ImportFromFileAsync(samplePath);

        Assert.Equal(1, firstRun.RecordsRead);
        Assert.Equal(1, firstRun.RecordsInserted);
        Assert.Equal(1, secondRun.RecordsRead);
        Assert.Equal(1, secondRun.RecordsSkipped);

        var initiative = await context.ProjectLaws
            .Include(x => x.ImportedAuthors)
            .Include(x => x.ImportedEvents)
            .Include(x => x.ImportedVotes)
                .ThenInclude(x => x.Blocks)
            .Include(x => x.ImportedDocuments)
            .Include(x => x.ImportedPublications)
            .Include(x => x.ImportedInterventions)
            .SingleAsync(x => x.SourceIdText == "356278");

        Assert.Equal("P", initiative.InitiativeTypeCode);
        Assert.Equal("Proposta de Lei", initiative.InitiativeTypeDescription);
        Assert.Contains(initiative.ImportedAuthors, x => x.AuthorKind == "Other" && x.Name == "Governo");
        Assert.Contains(initiative.ImportedVotes, x => x.Stage == "Generality");
        Assert.Contains(initiative.ImportedVotes, x => x.Stage == "Speciality");
        Assert.Contains(initiative.ImportedVotes, x => x.Stage == "FinalGlobal");
        Assert.Contains(initiative.ImportedDocuments, x => x.Scope == "InitiativeText");
        Assert.Contains(initiative.ImportedPublications, x => x.DiaryUrl != null);
        Assert.Contains(
            initiative.ImportedVotes.SelectMany(x => x.Blocks),
            x => x.PartyAcronym == "CDS-PP" && x.VotingOrientation == VotingOrientation.InFavor);
    }

    [Fact]
    public async Task ImportFromFileAsync_ImportsArraySample()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = CreateService(context);
        var samplePath = SamplePath("example_projeto_lei.json");

        var result = await service.ImportFromFileAsync(samplePath);

        Assert.Equal(3, result.RecordsRead);
        Assert.Equal(3, result.RecordsInserted);
        Assert.Equal(3, await context.ProjectLaws.CountAsync());
        Assert.Equal(2, await context.ProjectLaws.CountAsync(x => x.InitiativeTypeCode == "R"));
        Assert.Equal(1, await context.ProjectLaws.CountAsync(x => x.InitiativeTypeCode == "J"));
    }

    [Fact]
    public async Task DocumentRedaction_UsesDeputyAndPartyTermsFromBaseInfo()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var baseInfoPath = Path.GetTempFileName();
        var documentPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(
            baseInfoPath,
            """
            {
              "Deputados": [
                {
                  "DepCadId": 9008,
                  "DepId": 15760,
                  "DepNomeCompleto": "Francisco Gabriel Meneses de Lima",
                  "DepNomeParlamentar": "Francisco Lima",
                  "LegDes": "XVII",
                  "DepGP": [{ "gpSigla": "CH" }],
                  "DepSituacao": [{ "sioDes": "Efetivo" }]
                }
              ]
            }
            """);
        await File.WriteAllTextAsync(
            documentPath,
            "Projeto apresentado por Francisco Lima do CH para efeitos de teste.");

        var baseInfoService = new ParliamentBaseInfoImportService(
            context,
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<ParliamentBaseInfoImportService>.Instance);
        await baseInfoService.ImportFromFileAsync("XVII", baseInfoPath);

        var party = await context.PoliticalParties.SingleAsync(x => x.partyAcronym == "CH");
        var projectLaw = new ProjectLaw
        {
            SourceId = 999,
            SourceIdText = "999",
            Legislatura = "XVII",
            Score = 100,
            amountOfUsersInterested = 0,
            totalAmountOfVotesFromUsers = 0,
            VoteDate = "2026-01-01",
            ProposingParty = party,
            ProposalTitle = "Teste",
            FullProposalTextLink = documentPath
        };
        projectLaw.ImportedAuthors.Add(new ParliamentInitiativeAuthor
        {
            AuthorKind = "Deputy",
            Name = "Francisco Lima",
            Acronym = "CH"
        });
        projectLaw.ImportedDocuments.Add(new ParliamentInitiativeDocument
        {
            Scope = "InitiativeText",
            Name = "Texto da iniciativa",
            Url = documentPath
        });
        context.ProjectLaws.Add(projectLaw);
        await context.SaveChangesAsync();

        var redactionService = new ParliamentDocumentRedactionService(
            context,
            new HttpClient(),
            NullLogger<ParliamentDocumentRedactionService>.Instance);

        var result = await redactionService.ProcessInitiativeTextDocumentsAsync("XVII", projectLaw.Id, 1);

        Assert.Equal(1, result.DocumentsProcessed);
        var content = await context.ParliamentDocumentContents.SingleAsync();
        Assert.Contains("[redigido]", content.RedactedContentText);
        Assert.DoesNotContain("Francisco Lima", content.RedactedContentText);
        Assert.DoesNotContain("CH", content.RedactedContentText);
    }

    private static ParliamentOpenDataImportService CreateService(DatabaseContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new ParliamentOpenDataImportService(
            context,
            new HttpClient(),
            configuration,
            NullLogger<ParliamentOpenDataImportService>.Instance);
    }

    private static DatabaseContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase($"parliament-import-tests-{System.Guid.NewGuid()}")
            .Options;

        return new DatabaseContext(options);
    }

    private static string SamplePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../docs/samples",
            fileName));
    }
}
