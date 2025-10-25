using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Clean.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data;

public class DataContext : IdentityDbContext<User, IdentityRole<int>, int>, IDataContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<SalaryAnomaly> SalaryAnomalies { get; set; }
    public DbSet<SalaryHistory> SalaryHistories { get; set; }
    public DbSet<PayrollRecord> PayrollRecords { get; set; }
    public DbSet<VacationBalance> VacationBalances { get; set; }
    public DbSet<VacationRecord> VacationRecords { get; set; }

    public async Task MigrateAsync()    
    {
        await Database.MigrateAsync();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(UserConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(EmployeeConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DepartmentConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(VacationBalanceConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(VacationRecordConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(SalaryAnomalyConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(SalaryHistoryConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(PayrollRecordConfigurations).Assembly);
    } 
}