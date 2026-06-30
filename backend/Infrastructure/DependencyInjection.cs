using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Parlamento.Application.Abstractions;
using Parlamento.Infrastructure.Persistence;
using Parlamento.Infrastructure.Services;

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

        return services;
    }
}
