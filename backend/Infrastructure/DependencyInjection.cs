using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Parlamento.Application.Abstractions;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services;
using Parlamento.Infrastructure.Services.Documents;
using Parlamento.Infrastructure.Services.ParliamentOpenData;
using Parlamento.Infrastructure.Services.Summaries;

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
        services.AddScoped<IAppTokenService, JwtTokenService>();
        services.AddScoped<IPoliticalPartyService, PoliticalPartyService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IProposalFlowService, ProposalFlowService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVotingService, VotingService>();
        services.Configure<OpenAiSummaryOptions>(options =>
        {
            var section = configuration.GetSection(OpenAiSummaryOptions.SectionName);
            options.ApiKey = configuration["OPENAI_API_KEY"] ?? section["ApiKey"];
            options.Model = configuration["OPENAI_MODEL"] ?? section["Model"] ?? "gpt-4o-mini-2024-07-18";
            options.Temperature = double.TryParse(section["Temperature"], out var temperature)
                ? temperature
                : 0.1;
            options.MaxOutputTokens = int.TryParse(section["MaxOutputTokens"], out var maxOutputTokens)
                ? maxOutputTokens
                : 700;
        });
        services.Configure<AppJwtOptions>(options =>
        {
            var section = configuration.GetSection(AppJwtOptions.SectionName);
            options.Issuer = section["Issuer"] ?? "parlamento-app";
            options.Audience = section["Audience"] ?? "parlamento-app";
            options.SigningKey = configuration["JWT_SIGNING_KEY"] ?? section["SigningKey"] ?? string.Empty;
            options.ExpirationMinutes = int.TryParse(section["ExpirationMinutes"], out var expirationMinutes)
                ? expirationMinutes
                : 60;
        });
        services.Configure<ParliamentOpenDataOptions>(options =>
        {
            var section = configuration.GetSection(ParliamentOpenDataOptions.SectionName);
            options.LatestLegislature = section["LatestLegislature"];
            options.DailyImport.Enabled = bool.TryParse(section["DailyImport:Enabled"], out var enabled) && enabled;
            options.DailyImport.RunSummaries = !bool.TryParse(section["DailyImport:RunSummaries"], out var runSummaries) || runSummaries;
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
        services.AddScoped<IDocumentExtractor, ITextPdfDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, PdfDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, DocxDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, HtmlDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, TextDocumentExtractor>();
        services.AddScoped<IDocumentModelRedactor, DocumentModelRedactor>();
        services.AddScoped<IDocumentModelRenderer, DocumentModelRenderer>();
        services.AddScoped<IParliamentDocumentRedactionService>(provider =>
            new ParliamentDocumentRedactionService(
                provider.GetRequiredService<DatabaseContext>(),
                new HttpClient(),
                provider.GetServices<IDocumentExtractor>(),
                provider.GetRequiredService<IDocumentModelRedactor>(),
                provider.GetRequiredService<IDocumentModelRenderer>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ParliamentDocumentRedactionService>>()));
        services.AddScoped<ILegislativeSummaryClient>(provider =>
            new OpenAiLegislativeSummaryClient(
                new HttpClient(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiSummaryOptions>>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiLegislativeSummaryClient>>()));
        services.AddScoped<IParliamentSummaryService, ParliamentSummaryService>();
        services.AddScoped<IParliamentDataSeedService, ParliamentDataSeedService>();
        services.AddHostedService<DailyParliamentImportHostedService>();

        return services;
    }
}
