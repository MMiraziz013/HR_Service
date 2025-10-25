using Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Clean.Infrastructure.Data.Seed;

public class SeedDataInitializer
{
    private readonly IEnumerable<IDataSeeder> _seeders;
    private readonly ILogger<SeedDataInitializer> _logger;

    public SeedDataInitializer(IEnumerable<IDataSeeder> seeders, ILogger<SeedDataInitializer> logger)
    {
        _seeders = seeders;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("🔄 Starting data seeding process...");

        // Ensure a deterministic order for dependent seeders
        var orderedSeeders = _seeders.OrderBy(seeder => seeder switch
        {
            IdentitySeeder _ => 1,
            SeedAdminUser _ => 2,
            _ => 99
        });

        foreach (var seeder in orderedSeeders)
        {
            try
            {
                _logger.LogInformation("➡️ Running {Seeder}...", seeder.GetType().Name);
                await seeder.SeedAsync();
                _logger.LogInformation("✅ {Seeder} completed successfully.", seeder.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during {Seeder} execution.", seeder.GetType().Name);
            }
        }

        _logger.LogInformation("🎉 Data seeding process finished.");
    }
}