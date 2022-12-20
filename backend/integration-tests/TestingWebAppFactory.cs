using System;
using System.Linq;

using backend.Models;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace integration_tests;

public class TestingWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<DatabaseContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDatabaseTest");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<DatabaseContext>();
            var logger = scopedServices
                .GetRequiredService<ILogger<TestingWebAppFactory>>();

            db.Database.EnsureCreated();

            try
            {
                SeedDbForTests(db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the " +
                                    "database with test messages. Error: {Message}", ex.Message);
            }
        });
    }

    protected virtual void SeedDbForTests(DatabaseContext db)
    {
        // override this method to seed the database with test data
    }
}