using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Parlamento.Domain.Enums;
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

    private static ParliamentOpenDataImportService CreateService(DatabaseContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new ParliamentOpenDataImportService(context, new HttpClient(), configuration);
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
