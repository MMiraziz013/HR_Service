using Clean.Application.Abstractions;
using Clean.Infrastructure.Data.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clean.Infrastructure;

public static class InfrastructureInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDataSeeder, IdentitySeeder>();
        services.AddTransient<IDataSeeder, SeedAdminUser>();
        services.AddTransient<IDataSeeder, SeedHRUsers>();
        services.AddTransient<IDataSeeder, SeedEmployeeUsers>();
        services.AddTransient<SeedDataInitializer>();
        
        return services;
    }
}