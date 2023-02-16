using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class DatabaseContext : DbContext
{
    public DatabaseContext() { }

    public DatabaseContext(DbContextOptions options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoliticalParty>().HasData(new PoliticalParty
        {
            partyAcronym = "PSD",
            fullName = "Partido Social-Democrata"
        },
        new PoliticalParty
        {
            partyAcronym = "PS",
            fullName = "Partido Socialista"
        },
        new PoliticalParty
        {
            partyAcronym = "BE",
            fullName = "Bloco de Esquerda"
        },
        new PoliticalParty
        {
            partyAcronym = "PCP",
            fullName = "Partido Comunista Português"
        },
        new PoliticalParty
        {
            partyAcronym = "CDS-PP",
            fullName = "Coligação Democrática Unitária"
        },
        new PoliticalParty
        {
            partyAcronym = "PAN",
            fullName = "Pessoas-Animais-Natureza"
        },
        new PoliticalParty
        {
            partyAcronym = "L",
            fullName = "Partido Livre"
        },
        new PoliticalParty
        {
            partyAcronym = "IL",
            fullName = "Iniciativa Liberal"
        },
        new PoliticalParty
        {
            partyAcronym = "CH",
            fullName = "Chega"
        },
        new PoliticalParty
        {
            partyAcronym = "Governo",
            fullName = "Governo"
        }
        );

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();
        var databaseFilePath = configuration.GetSection("DatabaseFilePath").Value ?? "./db.sqlite";
        optionsBuilder.UseSqlite(@"Data Source=" + databaseFilePath + @";foreign keys=true;");
    }

    public DbSet<ProjectLaw>? ProjectLaws { get; set; }

    public DbSet<User>? Users { get; set; }

    public DbSet<PoliticalParty>? PoliticalParties { get; set; }


}