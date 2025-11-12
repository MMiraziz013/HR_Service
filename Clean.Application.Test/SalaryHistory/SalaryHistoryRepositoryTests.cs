using Clean.Domain.Entities;
using Clean.Infrastructure.Data;
using Clean.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Test.SalaryHistory;

public class SalaryHistoryRepositoryTests
{
    private readonly DataContext _context;
    private readonly SalaryHistoryRepository _repository;

    public SalaryHistoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;

        _context = new DataContext(options);
        _repository = new SalaryHistoryRepository(_context);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var department1 = new Department { Id = 1, Name = "IT",Description = "Another test 1"};
        var department2 = new Department { Id = 2, Name = "HR",Description = "Another test 2" };

        var employee1 = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = 1,
            Department = department1,
        };

        var employee2 = new Employee
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            DepartmentId = 2,
            Department = department2
        };

        _context.SalaryHistories.AddRange(
            new Domain.Entities.SalaryHistory
            {
                Id = 1,
                Employee = employee1,
                EmployeeId = 1,
                Month = new DateOnly(2025, 10, 1),
                BaseAmount = 1000,
                BonusAmount = 100
            },
            new Domain.Entities.SalaryHistory
            {
                Id = 2,
                Employee = employee1,
                EmployeeId = 1,
                Month = new DateOnly(2025, 11, 1),
                BaseAmount = 1200,
                BonusAmount = 150
            },
            new Domain.Entities.SalaryHistory
            {
                Id = 3,
                Employee = employee2,
                EmployeeId = 2,
                Month = new DateOnly(2025, 11, 1),
                BaseAmount = 900,
                BonusAmount = 50
            }
        );

        _context.SaveChanges();
    }

    
    [Fact]
    public async Task AddAsync_ShouldReturnTrue_WhenSalaryHistoryDoesNotExist()
    {
        var newSalary = new Domain.Entities.SalaryHistory
        {
            EmployeeId = 1,
            Month = new DateOnly(2025, 12, 1),
            BaseAmount = 1200,
            BonusAmount = 150
        };
        
        var result = await _repository.AddAsync(newSalary);
        Assert.True(result); 
        var added = await _context.SalaryHistories
            .FirstOrDefaultAsync(s => s.EmployeeId == 1 && s.Month == new DateOnly(2025, 12, 1));
        Assert.NotNull(added);
        Assert.Equal(1200, added.BaseAmount);
    }
    
    [Fact]
    public async Task AddAsync_ShouldReturnFalse_WhenSalaryHistoryAlreadyExists()
    {
        var existingSalary = new Domain.Entities.SalaryHistory
        {
            EmployeeId = 1,
            Month = new DateOnly(2025, 11, 1), 
            BaseAmount = 1100,
            BonusAmount = 200
        };
        
        var result = await _repository.AddAsync(existingSalary);
        
        Assert.False(result); 
        var count = await _context.SalaryHistories.CountAsync(s => s.EmployeeId == 1 && s.Month == new DateOnly(2025, 11, 1));
        Assert.Equal(1, count); 
    }
    
    
    
    [Fact]
    public async Task GetSalaryHistoryByEmployeeIdAsync_ShouldReturnEmptyList_WhenEmployeeDoesNotExist()
    {
        var result = await _repository.GetSalaryHistoryByEmployeeIdAsync(999); 
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetForReportAsync_ShouldReturnAll_WhenNoFilters()
    {
        var result = await _repository.GetForReportAsync(null, null, null, null);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }
    
    [Fact]
    public async Task GetForReportAsync_ShouldFilterByEmployeeId()
    {
        var result = (await _repository.GetForReportAsync(1, null, null, null)).ToList();

        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.NotNull(r.EmployeeName)); 
    }


    [Fact]
    public async Task GetTotalPaidAmountByDepartmentAsync_ShouldReturnCorrectSum_ForGivenDepartmentAndMonth()
    {
        var departmentId = 1;
        var month = new DateOnly(2025, 10, 1);
        
        var result=await _repository.GetTotalPaidAmountByDepartmentAsync(departmentId,month);
        
        Assert.Equal(1100,result);
    }
    
    [Fact]
    public async Task GetTotalPaidAmountByDepartmentAsync_ShouldReturnCorrectSum_WhenMultipleRecordsInMonth()
    {
        var departmentId = 1; 
        var month = new DateOnly(2025, 11, 1); 
        var result = await _repository.GetTotalPaidAmountByDepartmentAsync(departmentId, month);
        Assert.Equal(1350, result);
    }
    
    
    [Fact]
    public async Task GetTotalPaidAmountByDepartmentAsync_ShouldReturnZero_WhenNoRecordsMatch()
    {
        var departmentId = 2; 
        var month = new DateOnly(2025, 10, 1); 
        
        var result = await _repository.GetTotalPaidAmountByDepartmentAsync(departmentId, month);
        
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task UpdateSalaryHistoryAsync_ShouldUpdateBaseAmount_WhenRecordExists()
    {
        var salaryToUpdate = new Domain.Entities.SalaryHistory
        {
            EmployeeId = 1,
            Month = new DateOnly(2025, 11, 1),
            BaseAmount = 2000
        };
        
        var result = await _repository.UpdateSalaryAsync(salaryToUpdate);
        
        Assert.True(result);
        
        var updatedSalary = await _context.SalaryHistories
            .FirstOrDefaultAsync(s => s.EmployeeId == 1 && s.Month == new DateOnly(2025, 11, 1));
        Assert.NotNull(updatedSalary);
        Assert.Equal(2000, updatedSalary.BaseAmount);
    }
    
    [Fact]
    public async Task UpdateSalaryAsync_ShouldReturnFalse_WhenRecordDoesNotExist()
    {
        var salaryToUpdate = new Domain.Entities.SalaryHistory
        {
            EmployeeId = 1,
            Month = new DateOnly(2025, 12, 1), 
            BaseAmount = 3000
        };
        
        var result = await _repository.UpdateSalaryAsync(salaryToUpdate);
        
        Assert.False(result);
        
        var anyUpdated = await _context.SalaryHistories
            .AnyAsync(s => s.EmployeeId == 1 && s.Month == new DateOnly(2025, 12, 1));
        Assert.False(anyUpdated);
    }
}