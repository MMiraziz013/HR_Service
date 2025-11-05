using Clean.Application.Abstractions;
using Clean.Infrastructure.Data;
using Clean.Infrastructure.Data.Repositories;
using Clean.Infrastructure.Data.Seed;
using Clean.Infrastructure.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Clean.Infrastructure;

public static class InfrastructureInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var baseConnectionString = configuration.GetConnectionString("DefaultConnection");

        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(dbPassword))
        {
            baseConnectionString = baseConnectionString.Replace("Password=", $"Password={dbPassword}");
        }

        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(baseConnectionString));

        services.AddScoped<IDataContext>(provider => provider.GetRequiredService<DataContext>());

        services.AddTransient<IDataSeeder, IdentitySeeder>();
        services.AddTransient<IDataSeeder, SeedAdminUser>();
        //TODO: Check if we need to seed hrs and employees. If yes, employees table should also be updated!
        // services.AddTransient<IDataSeeder, SeedHRUsers>();
        services.AddTransient<IDataSeeder, SeedDepartments>();
        services.AddTransient<IDataSeeder, SeedEmployeeUsers>();
        services.AddTransient<SeedDataInitializer>();
        
        // Repository Injections 
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IEmployeeRepository, EmployeeRepository>();
        services.AddTransient<IDepartmentRepository, DepartmentRepository>();
        services.AddTransient<IVacationBalanceRepository, VacationBalanceRepository>();
        services.AddTransient<IVacationRecordRepository, VacationRecordRepository>();
        
        // Redis Registration
        services.AddTransient<ICacheService, RedisCacheService>();
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionStringRedis = configuration["Redis:ConnectionString"];
            return ConnectionMultiplexer.Connect($"{connectionStringRedis},abortConnect=false");
        });

        
        services.AddTransient<ISalaryHistoryRepository, SalaryHistoryRepository>();
        services.AddTransient<IPayrollRecordRepository, PayrollRecordRepository>();
        services.AddTransient<ISalaryAnomalyRepository, SalaryAnomalyRepository>();
        return services;
    }

}