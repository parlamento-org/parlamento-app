using Microsoft.EntityFrameworkCore;

using Parlamento.Domain.Entities;

namespace Parlamento.Infrastructure.Persistence;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoliticalParty>().HasData(
            new PoliticalParty
            {
                partyAcronym = "PSD",
                fullName = "Partido Social-Democrata",
                logoLink = "https://pt.wikipedia.org/wiki/Ficheiro:Logo_PSD_cor.PNG"
            },
            new PoliticalParty
            {
                partyAcronym = "PS",
                fullName = "Partido Socialista",
                logoLink = "https://pt.wikipedia.org/wiki/Ficheiro:Partido_Socialista_%28Portugal%29.png"
            },
            new PoliticalParty
            {
                partyAcronym = "BE",
                fullName = "Bloco de Esquerda",
                logoLink = "https://pt.wikipedia.org/wiki/Bloco_de_Esquerda"
            },
            new PoliticalParty
            {
                partyAcronym = "PCP",
                fullName = "Partido Comunista Português",
                logoLink = "https://pt.m.wikipedia.org/wiki/Ficheiro:Portuguese_Communist_Party_logo.svg"
            },
            new PoliticalParty
            {
                partyAcronym = "CDSPP",
                fullName = "Coligação Democrática Unitária",
                logoLink = "https://pt.m.wikipedia.org/wiki/Ficheiro:CDS_%E2%80%93_People%27s_Party_logo.svg"
            },
            new PoliticalParty
            {
                partyAcronym = "PAN",
                fullName = "Pessoas-Animais-Natureza",
                logoLink = "https://pt.wikipedia.org/wiki/Pessoas%E2%80%93Animais%E2%80%93Natureza"
            },
            new PoliticalParty
            {
                partyAcronym = "L",
                fullName = "Partido Livre",
                logoLink = "https://pt.m.wikipedia.org/wiki/Ficheiro:Partido_LIVRE_logo.png"
            },
            new PoliticalParty
            {
                partyAcronym = "IL",
                fullName = "Iniciativa Liberal",
                logoLink = "https://pt.m.wikipedia.org/wiki/Ficheiro:Iniciativa_Liberal_logo_1.png"
            },
            new PoliticalParty
            {
                partyAcronym = "CH",
                fullName = "Chega",
                logoLink = "https://en.wikipedia.org/wiki/File:Logo_of_the_Chega_(political_party).svg"
            },
            new PoliticalParty
            {
                partyAcronym = "Governo",
                fullName = "Governo",
                logoLink = "https://pt.wikipedia.org/wiki/Ficheiro:Trollface.png"
            });

        modelBuilder.Entity<ProjectLaw>()
            .HasIndex(x => x.SourceId);

        modelBuilder.Entity<ProjectLaw>()
            .HasIndex(x => x.SourceIdText);

        modelBuilder.Entity<ProjectLaw>()
            .HasOne(x => x.LastImportRun)
            .WithMany()
            .HasForeignKey(x => x.LastImportRunId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ParliamentImportRun>()
            .HasIndex(x => new { x.SourceKind, x.Legislature, x.StartedAtUtc });

        modelBuilder.Entity<ParliamentImportSkip>()
            .HasOne(x => x.ParliamentImportRun)
            .WithMany(x => x.Skips)
            .HasForeignKey(x => x.ParliamentImportRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentImportError>()
            .HasOne(x => x.ParliamentImportRun)
            .WithMany(x => x.Errors)
            .HasForeignKey(x => x.ParliamentImportRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeAuthor>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.ImportedAuthors)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeEvent>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.ImportedEvents)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeEvent>()
            .HasIndex(x => new { x.ProjectLawId, x.PhaseCode, x.PhaseDate });

        modelBuilder.Entity<ParliamentInitiativeVote>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.ImportedVotes)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeVote>()
            .HasOne(x => x.ParliamentInitiativeEvent)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.ParliamentInitiativeEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeVote>()
            .HasIndex(x => new { x.ProjectLawId, x.Stage, x.VoteDate });

        modelBuilder.Entity<ParliamentInitiativeVoteBlock>()
            .HasOne(x => x.ParliamentInitiativeVote)
            .WithMany(x => x.Blocks)
            .HasForeignKey(x => x.ParliamentInitiativeVoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeDocument>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.ImportedDocuments)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeDocument>()
            .HasOne(x => x.ParliamentInitiativeEvent)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.ParliamentInitiativeEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentDocumentContent>()
            .HasOne(x => x.ProjectLaw)
            .WithMany()
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentDocumentContent>()
            .HasOne(x => x.ParliamentInitiativeDocument)
            .WithOne(x => x.Content)
            .HasForeignKey<ParliamentDocumentContent>(x => x.ParliamentInitiativeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentDocumentContent>()
            .HasIndex(x => x.ParliamentInitiativeDocumentId)
            .IsUnique();

        modelBuilder.Entity<ParliamentSummary>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.Summaries)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentSummary>()
            .HasOne(x => x.ParliamentDocumentContent)
            .WithMany()
            .HasForeignKey(x => x.ParliamentDocumentContentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentSummary>()
            .HasIndex(x => new { x.ProjectLawId, x.SourceDocumentHash, x.ModelName, x.PromptVersion });

        modelBuilder.Entity<ParliamentDeputy>()
            .HasIndex(x => new { x.Legislature, x.SourceDeputyId });

        modelBuilder.Entity<ParliamentDeputy>()
            .HasIndex(x => new { x.Legislature, x.SourceCadId });

        modelBuilder.Entity<ParliamentaryGroup>()
            .HasIndex(x => new { x.Legislature, x.Acronym })
            .IsUnique();

        modelBuilder.Entity<ParliamentRedactionTerm>()
            .HasIndex(x => new { x.Legislature, x.Term });

        modelBuilder.Entity<ParliamentInitiativePublication>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.ImportedPublications)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativePublication>()
            .HasOne(x => x.ParliamentInitiativeEvent)
            .WithMany(x => x.Publications)
            .HasForeignKey(x => x.ParliamentInitiativeEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeIntervention>()
            .HasOne(x => x.ProjectLaw)
            .WithMany(x => x.ImportedInterventions)
            .HasForeignKey(x => x.ProjectLawId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ParliamentInitiativeIntervention>()
            .HasOne(x => x.ParliamentInitiativeEvent)
            .WithMany(x => x.Interventions)
            .HasForeignKey(x => x.ParliamentInitiativeEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public DbSet<ProjectLaw> ProjectLaws { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<PoliticalParty> PoliticalParties { get; set; } = default!;
    public DbSet<ParliamentImportRun> ParliamentImportRuns { get; set; } = default!;
    public DbSet<ParliamentImportSkip> ParliamentImportSkips { get; set; } = default!;
    public DbSet<ParliamentImportError> ParliamentImportErrors { get; set; } = default!;
    public DbSet<ParliamentInitiativeAuthor> ParliamentInitiativeAuthors { get; set; } = default!;
    public DbSet<ParliamentInitiativeEvent> ParliamentInitiativeEvents { get; set; } = default!;
    public DbSet<ParliamentInitiativeVote> ParliamentInitiativeVotes { get; set; } = default!;
    public DbSet<ParliamentInitiativeVoteBlock> ParliamentInitiativeVoteBlocks { get; set; } = default!;
    public DbSet<ParliamentInitiativeDocument> ParliamentInitiativeDocuments { get; set; } = default!;
    public DbSet<ParliamentInitiativePublication> ParliamentInitiativePublications { get; set; } = default!;
    public DbSet<ParliamentInitiativeIntervention> ParliamentInitiativeInterventions { get; set; } = default!;
    public DbSet<ParliamentDocumentContent> ParliamentDocumentContents { get; set; } = default!;
    public DbSet<ParliamentSummary> ParliamentSummaries { get; set; } = default!;
    public DbSet<ParliamentDeputy> ParliamentDeputies { get; set; } = default!;
    public DbSet<ParliamentaryGroup> ParliamentaryGroups { get; set; } = default!;
    public DbSet<ParliamentRedactionTerm> ParliamentRedactionTerms { get; set; } = default!;
}
