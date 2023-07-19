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