using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Parlamento.Application.Abstractions;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services;
using Parlamento.Infrastructure.Services.ParliamentOpenData;

namespace Parlamento.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("DefaultConnection");
        var sqliteConnectionString = configuration.GetConnectionString("Sqlite");
        var databaseFilePath = configuration.GetSection("DatabaseFilePath").Value ?? "./db.sqlite";

        services.AddDbContext<DatabaseContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(postgresConnectionString))
            {
                options.UseNpgsql(postgresConnectionString, npgsql => npgsql.MigrationsAssembly("Parlamento.Infrastructure"));
                return;
            }

            if (!string.IsNullOrWhiteSpace(sqliteConnectionString))
            {
                options.UseSqlite(sqliteConnectionString, sqlite => sqlite.MigrationsAssembly("backend"));
                return;
            }

            options.UseSqlite(
                $@"Data Source={databaseFilePath};foreign keys=true;",
                sqlite => sqlite.MigrationsAssembly("backend"));
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPoliticalPartyService, PoliticalPartyService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVotingService, VotingService>();
        services.Configure<ParliamentOpenDataOptions>(options =>
        {
            var section = configuration.GetSection(ParliamentOpenDataOptions.SectionName);
            options.LatestLegislature = section["LatestLegislature"];
            options.DailyImport.Enabled = bool.TryParse(section["DailyImport:Enabled"], out var enabled) && enabled;
            options.DailyImport.TimeZoneId = section["DailyImport:TimeZoneId"] ?? "Europe/Lisbon";
            options.DailyImport.RunAt = TimeSpan.TryParse(section["DailyImport:RunAt"], out var runAt)
                ? runAt
                : TimeSpan.Zero;

            options.Legislatures = section
                .GetSection("Legislatures")
                .GetChildren()
                .ToDictionary(
                    child => child.Key,
                    child => new ParliamentLegislatureSourceOptions
                    {
                        InitiativesUrl = child["InitiativesUrl"],
                        BaseInfoUrl = child["BaseInfoUrl"]
                    });
        });
        services.AddScoped<IParliamentOpenDataImportService>(provider =>
            new ParliamentOpenDataImportService(
                provider.GetRequiredService<DatabaseContext>(),
                new HttpClient(),
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ParliamentOpenDataImportService>>()));
        services.AddScoped<IParliamentBaseInfoImportService>(provider =>
            new ParliamentBaseInfoImportService(
                provider.GetRequiredService<DatabaseContext>(),
                new HttpClient(),
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ParliamentBaseInfoImportService>>()));
        services.AddScoped<IParliamentDocumentRedactionService>(provider =>
            new ParliamentDocumentRedactionService(
                provider.GetRequiredService<DatabaseContext>(),
                new HttpClient(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ParliamentDocumentRedactionService>>()));
        services.AddHostedService<DailyParliamentImportHostedService>();

        return services;
    }
}
