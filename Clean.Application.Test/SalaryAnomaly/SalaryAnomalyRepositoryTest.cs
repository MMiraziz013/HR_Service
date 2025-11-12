using Clean.Domain.Entities;
using Clean.Infrastructure.Data;
using Clean.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Test.SalaryAnomaly;

public class SalaryAnomalyRepositoryTest
{
    private readonly DataContext _context;
    private readonly SalaryAnomalyRepository _repository;

    public SalaryAnomalyRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;

        _context = new DataContext(options);
        _repository = new SalaryAnomalyRepository(_context);

        SeedDatabase();
    }
    
    private void SeedDatabase()
    {
        var dept1 = new Department { Id = 1, Name = "IT",Description = "IT Department" };
        var dept2 = new Department { Id = 2, Name = "HR" ,Description = "HR Department" };

        var emp1 = new Employee { Id = 1, FirstName = "John", LastName = "Doe", Department = dept1, DepartmentId = dept1.Id };
        var emp2 = new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", Department = dept2, DepartmentId = dept2.Id };

        _context.SalaryAnomalies.AddRange(
            new Domain.Entities.SalaryAnomaly
            {
                Id = 1,
                Employee = emp1,
                EmployeeId = emp1.Id,
                Month = new DateOnly(2025, 10, 1),
                ActualAmount = 1200,
                ExpectedAmount = 1000,
                DeviationPercent = 20,
                IsReviewed = false,
                ReviewComment = null
            },
            new Domain.Entities.SalaryAnomaly
            {
                Id = 2,
                Employee = emp2,
                EmployeeId = emp2.Id,
                Month = new DateOnly(2025, 11, 1),
                ActualAmount = 900,
                ExpectedAmount = 1000,
                DeviationPercent = -10,
                IsReviewed = true,
                ReviewComment = "Checked"
            }
        );

        _context.SaveChanges();
    }
    
    [Fact]
    public async Task GetForReportAsync_ReturnsAll_WhenNoFilter()
    {
   
        var result = await _repository.GetForReportAsync(null, null, null, null, null);
        
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task GetForReportAsync_FiltersByEmployeeId()
    {
        
        var result = await _repository.GetForReportAsync(1, null, null, null, null);
       
        Assert.Single(result);
        Assert.Equal(1, result.First().EmployeeId);
        Assert.Equal("John Doe", result.First().EmployeeName);
    }
    
    [Fact]
    public async Task GetForReportAsync_FiltersByDepartmentId()
    {
        var result = await _repository.GetForReportAsync(null, 2, null, null, null);
        
        Assert.Single(result);
        Assert.Equal(2, result.First().DepartmentId);
        Assert.Equal("HR", result.First().DepartmentName);
    }
    
    [Fact]
    public async Task GetForReportAsync_FiltersByMonthRange()
    {
        var result = await _repository.GetForReportAsync(null, null, new DateOnly(2025, 11, 1), null, null);
        
        Assert.Single(result);
        Assert.Equal(2, result.First().Id);
    }

    [Fact]
    public async Task GetForReportAsync_FiltersByIsReviewed()
    {
        var result = await _repository.GetForReportAsync(null, null, null, null, true);
        
        Assert.Single(result);
        Assert.True(result.First().IsReviewed);
    }
    
    [Fact]
    public async Task GetUnviewedAsync_ReturnsOnlyUnreviewedAnomalies()
    {
        var result = await _repository.GetUnviewedAsync();
        
        Assert.Single(result);
        Assert.False(result.First().IsReviewed);
        Assert.Equal(1, result.First().Id);
        Assert.Equal("John Doe", $"{result.First().Employee.FirstName} {result.First().Employee.LastName}");
    }
    
    [Fact]
    public async Task GetUnviewedAsync_ReturnsEmpty_WhenNoUnreviewedAnomalies()
    {
        var allAnomalies = await _context.SalaryAnomalies.ToListAsync();
        foreach (var a in allAnomalies) a.IsReviewed = true;
        await _context.SaveChangesAsync();
        
        var result = await _repository.GetUnviewedAsync();
        
        Assert.Empty(result);
    }
}