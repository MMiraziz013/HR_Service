using Clean.Application.Abstractions;

namespace Clean.Infrastructure.Data.Seed;

public class SeedDataInitializer
{
    private readonly IEnumerable<IDataSeeder> _seeders;

    public SeedDataInitializer(IEnumerable<IDataSeeder> seeders)
    {
        _seeders = seeders;
    }

    public async Task InitializeAsync()
    {
        foreach (var seeder in _seeders)
        {
            await seeder.SeedAsync();
        }
    }
}
