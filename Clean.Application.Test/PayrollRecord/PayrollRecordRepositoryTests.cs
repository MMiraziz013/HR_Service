using Clean.Domain.Entities;
using Clean.Infrastructure.Data;
using Clean.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Test.PayrollRecord;

public class PayrollRecordRepositoryTests
{
    private readonly DataContext _context;
    private readonly PayrollRecordRepository _repository;
    
    
    public PayrollRecordRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;

        _context = new DataContext(options);
        _repository = new PayrollRecordRepository(_context);

        SeedDatabase();
    }
    
    private void SeedDatabase()
    {
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = 1,
                FirstName = "Alice",
                LastName = "Johnson",
                DepartmentId = 1
            },
            new Employee
            {
                Id = 2,
                FirstName = "Bob",
                LastName = "Smith",
                DepartmentId = 2
            }
        };

        var payrolls = new List<Domain.Entities.PayrollRecord>
        {
            new Domain.Entities.PayrollRecord
            {
                Id = 1,
                EmployeeId = 1,
                PeriodStart = new DateOnly(2025, 4, 1),
                PeriodEnd = new DateOnly(2025, 4, 30),
                GrossPay = 2000m,
                Deductions = 200m,
                CreatedAt = DateTime.UtcNow
            },
            new Domain.Entities.PayrollRecord
            {
                Id = 2,
                EmployeeId = 1,
                PeriodStart = new DateOnly(2025, 5, 1),
                PeriodEnd = new DateOnly(2025, 5, 31),
                GrossPay = 2100m,
                Deductions = 150m,
                CreatedAt = DateTime.UtcNow
            },
            new Domain.Entities.PayrollRecord
            {
                Id = 3,
                EmployeeId = 2,
                PeriodStart = new DateOnly(2025, 5, 1),
                PeriodEnd = new DateOnly(2025, 5, 31),
                GrossPay = 2500m,
                Deductions = 300m,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Employees.AddRange(employees);
        _context.PayrollRecords.AddRange(payrolls);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPayrollRecord_WhenExists()
    {
        var result = await _repository.GetByIdAsync(1);
        
        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal(2000m, result.GrossPay);
        Assert.NotNull(result.Employee);
        Assert.Equal("Alice", result.Employee.FirstName);
    }
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(999);
        
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsAllPayrollRecords_InDescendingOrder()
    {
        var result = await _repository.GetAllAsync();
        
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result[0].PeriodStart > result[1].PeriodStart || result[0].PeriodStart > result[2].PeriodStart);
        Assert.NotNull(result[0].Employee);
        Assert.NotNull(result[1].Employee);
        Assert.Equal("Alice", result.First(r => r.Id == 2).Employee.FirstName);
        Assert.Equal("Bob", result.First(r => r.Id == 3).Employee.FirstName);
    }
    
    [Fact]
    public async Task GetTotalPaidForMonth_ReturnsCorrectSum()
    {
        var april2025 = new DateOnly(2025, 4, 1);
        var may2025 = new DateOnly(2025, 5, 1);
        
        var totalApril = await _repository.GetTotalPaidForMonth(april2025);
        var totalMay = await _repository.GetTotalPaidForMonth(may2025);
        
        Assert.Equal(1800m, totalApril); // 2000 - 200
        Assert.Equal((2100m - 150m) + (2500m - 300m), totalMay); // 1950 + 2200 = 4150
    }
    
    [Fact]
    public async Task GetTotalPaidForMonth_ReturnsZero_WhenNoRecords()
    {
        var june2025 = new DateOnly(2025, 6, 1);
        
        var totalJune = await _repository.GetTotalPaidForMonth(june2025);


        Assert.Equal(0m, totalJune);
    }

}