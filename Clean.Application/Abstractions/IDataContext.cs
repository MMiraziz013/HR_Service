using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Clean.Application.Abstractions;

public interface IDataContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }

    public DbSet<SalaryAnomaly> SalaryAnomalies { get; set; }

    public DbSet<SalaryHistory> SalaryHistories { get; set; }

    public DbSet<PayrollRecord> PayrollRecords { get; set; }

    public DbSet<VacationBalance> VacationBalances { get; set; }

    public DbSet<VacationRecord> VacationRecords { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task MigrateAsync();
    
    DatabaseFacade Database { get; }
}