using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class DatabaseContext : DbContext
{
    public DatabaseContext() { }

    public DatabaseContext(DbContextOptions options)
        : base(options)
    { }

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