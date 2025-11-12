using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;
using Clean.Application.Services.SalaryHistory;
using Clean.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Clean.Application.Test.SalaryHistory;

public class SalaryHistoryServiceTests
{
    private readonly Mock<ISalaryHistoryRepository> _repoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IDepartmentRepository> _deptRepoMock;
    private readonly Mock<ILogger<SalaryHistoryService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly SalaryHistoryService _service;

    public SalaryHistoryServiceTests()
    {
        _repoMock = new Mock<ISalaryHistoryRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _deptRepoMock = new Mock<IDepartmentRepository>();
        _loggerMock = new Mock<ILogger<SalaryHistoryService>>();
        _cacheMock = new Mock<ICacheService>();

        _service = new SalaryHistoryService(
            _repoMock.Object,
            _employeeRepoMock.Object,
            _deptRepoMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task AddSalaryHistoryAsync_ShouldReturnOK_WhenEmployeeIsFound()
    {
        var dto = new AddSalaryHistoryDto { EmployeeId = 54, BaseAmount = 1200m };
        
        var employee = new Employee
        {
            Id = 54,
            FirstName = "John",
            LastName = "Doe",
            SalaryHistories = new List<Domain.Entities.SalaryHistory>()
        };
        var employeeRepoMock = new Mock<IEmployeeRepository>();
        employeeRepoMock.Setup(r => r.GetEmployeeByIdAsync(54))
            .ReturnsAsync(employee);
        
        var salaryRepoMock = new Mock<ISalaryHistoryRepository>();
        salaryRepoMock.Setup(r => r.ExistForMonth(It.IsAny<int>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(false); 
        salaryRepoMock.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.SalaryHistory>()))
            .ReturnsAsync(true); 
        
        var departmentRepoMock = new Mock<IDepartmentRepository>();
        
        var loggerMock = new Mock<ILogger<SalaryHistoryService>>();
        
        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        var service = new SalaryHistoryService(
            salaryRepoMock.Object,
            employeeRepoMock.Object,
            departmentRepoMock.Object,
            loggerMock.Object,
            cacheMock.Object
        );
        
        var result = await service.AddSalaryHistoryAsync(dto);
        
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Salary history added successfully.", result.Message);
        
        salaryRepoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.SalaryHistory>()), Times.Once);
        cacheMock.Verify(c => c.RemoveByPatternAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddSalaryHistoryAsync_ShouldReturnBadRequest_WhenEmployeeNotFound()
    {
        var dto = new AddSalaryHistoryDto { EmployeeId = 123, BaseAmount = 1000m };
        _employeeRepoMock.Setup(r => r.GetEmployeeByIdAsync(123))
            .ReturnsAsync((Employee)null!);
        
        var result = await _service.AddSalaryHistoryAsync(dto);
        
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Employee with this id is not found", result.Message);
    }
    
    
    /// <summary>
    /// Returns cached data if available
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnCachedData_WhenCacheExists()
    {
        var filter = new SalaryHistoryFilter();
        var cachedResponse = new PaginatedResponse<GetSalaryHistoryDto>(
            new List<GetSalaryHistoryDto> { new() { Id = 1 } }, 1, 1, 1);

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<PaginatedResponse<GetSalaryHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync(cachedResponse);

        var repoMock = new Mock<ISalaryHistoryRepository>(); 
        var service = new SalaryHistoryService(repoMock.Object, 
            Mock.Of<IEmployeeRepository>(),
            Mock.Of<IDepartmentRepository>(),
            Mock.Of<ILogger<SalaryHistoryService>>(),
            cacheMock.Object);
        
        var result = await service.GetAllAsync(filter);
        
        Assert.Equal(200, result.StatusCode); 
        Assert.Single(result.Data!);
        repoMock.Verify(r => r.GetSalaryHistoriesAsync(It.IsAny<SalaryHistoryFilter>()), Times.Never);
    }

    /// <summary>
    /// Returns an empty list if the repository has no data
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnNotFound_WhenNoSalariesExist()
    {
        var filter = new SalaryHistoryFilter();

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<PaginatedResponse<GetSalaryHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((PaginatedResponse<GetSalaryHistoryDto>)null!);

        var repoMock = new Mock<ISalaryHistoryRepository>();
        repoMock.Setup(r => r.GetSalaryHistoriesAsync(filter))
            .ReturnsAsync(new List<Domain.Entities.SalaryHistory>()); 

        var service = new SalaryHistoryService(repoMock.Object,
            Mock.Of<IEmployeeRepository>(),
            Mock.Of<IDepartmentRepository>(),
            Mock.Of<ILogger<SalaryHistoryService>>(),
            cacheMock.Object);

  
        var result = await service.GetAllAsync(filter);
        
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(result.Data!);
    }

    /// <summary>
    /// Returns mapped salaries when data exists
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedData_WhenSalariesExist()
    {
        // Arrange
        var filter = new SalaryHistoryFilter();

        var salaryList = new List<Domain.Entities.SalaryHistory>
        {
            new Domain.Entities.SalaryHistory
            {
                Id = 1,
                EmployeeId = 10,
                Month = DateOnly.FromDateTime(DateTime.UtcNow),
                BaseAmount = 1000m,
                BonusAmount = 100m,
                Employee = new Employee { FirstName = "John", LastName = "Doe" }
            }
        };

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<PaginatedResponse<GetSalaryHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((PaginatedResponse<GetSalaryHistoryDto>)null!);

        cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PaginatedResponse<GetSalaryHistoryDto>>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        var repoMock = new Mock<ISalaryHistoryRepository>();
        repoMock.Setup(r => r.GetSalaryHistoriesAsync(filter))
            .ReturnsAsync(salaryList);

        var service = new SalaryHistoryService(repoMock.Object,
            Mock.Of<IEmployeeRepository>(),
            Mock.Of<IDepartmentRepository>(),
            Mock.Of<ILogger<SalaryHistoryService>>(),
            cacheMock.Object);
        
        var result = await service.GetAllAsync(filter);
        
        Assert.Equal(200, result.StatusCode);
        Assert.Single(result.Data!);
        Assert.Equal("John Doe", result.Data!.First().EmployeeName);
        cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PaginatedResponse<GetSalaryHistoryDto>>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    
    /// <summary>
    /// Returns salary by month if cache exists
    /// </summary>
    [Fact]
    public async Task GetSalaryHistoryByMonthAsync_ShouldReturnCachedData_WhenCacheExists()
    {
        var month = new DateTime(2025, 11, 1);
        var cached = new Response<List<GetSalaryHistoryWithEmployeeDto>>(
            HttpStatusCode.OK, "cached", new List<GetSalaryHistoryWithEmployeeDto> { new() { Id = 1 } });

        _cacheMock.Setup(c => c.GetAsync<Response<List<GetSalaryHistoryWithEmployeeDto>>>(It.IsAny<string>()))
            .ReturnsAsync(cached);

        var result = await _service.GetSalaryHistoryByMonthAsync(month);

        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Data!);
        _repoMock.Verify(r => r.GetByMonthAsync(It.IsAny<DateTime>()), Times.Never);
    }
    
    /// <summary>
    /// Returns a not found error because there is no data
    /// </summary>
    [Fact]
    public async Task GetSalaryHistoryByMonthAsync_ShouldReturnNotFound_WhenNoData()
    {
        var month = new DateTime(2025, 11, 1);
        _cacheMock.Setup(c => c.GetAsync<Response<List<GetSalaryHistoryWithEmployeeDto>>>(It.IsAny<string>()))
            .ReturnsAsync((Response<List<GetSalaryHistoryWithEmployeeDto>>?)null);
        _repoMock.Setup(r => r.GetByMonthAsync(month)).ReturnsAsync(new List<Domain.Entities.SalaryHistory>());

        var result = await _service.GetSalaryHistoryByMonthAsync(month);

        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Data);
    }
    
    /// <summary>
    /// returns latest salary histories when cache exists
    /// </summary>
    [Fact]
    public async Task GetLatestSalaryHistoriesAsync_ShouldReturnCachedData_WhenCacheExists()
    {
        var cachedResponse = new Response<List<GetSalaryHistoryDto>>(
            HttpStatusCode.OK, new List<GetSalaryHistoryDto> { new() { Id = 1 } });

        _cacheMock.Setup(c => c.GetAsync<Response<List<GetSalaryHistoryDto>>>(It.IsAny<string>()))
            .ReturnsAsync(cachedResponse);

        var result = await _service.GetLatestSalaryHistoriesAsync();

        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Data!);
        _employeeRepoMock.Verify(r => r.GetActiveEmployeesAsync(), Times.Never);
    }
    
    /// <summary>
    /// filters based on month 
    /// </summary>
    [Fact]
    public async Task GetLatestSalaryHistoriesAsync_ShouldFilterByEmployeeId_WhenFilterProvided()
    {
        _cacheMock.Setup(c => c.GetAsync<Response<List<GetSalaryHistoryDto>>>(It.IsAny<string>()))
            .ReturnsAsync((Response<List<GetSalaryHistoryDto>>?)null);

        var month = new DateOnly(2025, 11, 1);
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                SalaryHistories = new List<Domain.Entities.SalaryHistory>
                {
                    new Domain.Entities.SalaryHistory { Id = 10, Month = month, BaseAmount = 1000, BonusAmount = 100, EmployeeId = 1 }
                }
            },
            new Employee
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                SalaryHistories = new List<Domain.Entities.SalaryHistory>
                {
                    new Domain.Entities.SalaryHistory { Id = 11, Month = month, BaseAmount = 2000, BonusAmount = 200, EmployeeId = 2 }
                }
            }
        };
        employees[0].SalaryHistories[0].Employee = employees[0];
        employees[1].SalaryHistories[0].Employee = employees[1];

        _employeeRepoMock.Setup(r => r.GetActiveEmployeesAsync()).ReturnsAsync(employees);

        var filter = new SalaryHistoryFilter { EmployeeId = 2 };
        var result = await _service.GetLatestSalaryHistoriesAsync(filter);

        Assert.Single(result.Data!);
        Assert.Equal("Jane Smith", result.Data!.First().EmployeeName);
    }
    
    /// <summary>
    /// applies bonuses based on department when employees exist
    /// </summary>
    [Fact]
    public async Task ApplyDepartmentBonusAsync_ShouldApplyBonus_WhenEmployeesExist()
    {
        var departmentId = 1;
        var bonusPercentage = 10m;
        var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);

        var employees = new List<Employee>
        {
            new Employee { Id = 1, FirstName = "John", LastName = "Doe" }
        };

        var salary = new Domain.Entities.SalaryHistory
        {
            Id = 1,
            EmployeeId = 1,
            Month = currentMonth,
            BaseAmount = 1000m,
            BonusAmount = 0m
        };

        var employeeRepoMock = new Mock<IEmployeeRepository>();
        employeeRepoMock.Setup(r => r.GetActiveEmployeesByDepartmentAsync(departmentId))
            .ReturnsAsync(employees);

        var repoMock = new Mock<ISalaryHistoryRepository>();
        repoMock.Setup(r => r.GetSalaryByMonth(1, currentMonth))
            .ReturnsAsync(salary);
        repoMock.Setup(r => r.UpdateSalaryAsync(It.IsAny<Domain.Entities.SalaryHistory>()))
            .Returns(Task.FromResult(true));

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = new SalaryHistoryService(repoMock.Object,
            employeeRepoMock.Object,
            Mock.Of<IDepartmentRepository>(),
            Mock.Of<ILogger<SalaryHistoryService>>(),
            cacheMock.Object);

      
        var result = await service.ApplyDepartmentBonusAsync(departmentId, bonusPercentage);
        
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Data!);
        Assert.Equal(1000m, result.Data![0].BaseAmount);
        Assert.Equal(100m, result.Data![0].BonusAmount); 
        repoMock.Verify(r => r.UpdateSalaryAsync(It.IsAny<Domain.Entities.SalaryHistory>()), Times.Once);
        cacheMock.Verify(c => c.RemoveByPatternAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }
    
    /// <summary>
    /// applies bonuses based on departments and skips employees whose salary is not found
    /// </summary>
    [Fact]
    public async Task ApplyDepartmentBonusAsync_ShouldSkipEmployee_WhenSalaryIsNull()
    {
        var departmentId = 1;
        var bonusPercentage = 10m;
        var employees = new List<Employee>
        {
            new Employee { Id = 1, FirstName = "John", LastName = "Doe" }
        };

        var employeeRepoMock = new Mock<IEmployeeRepository>();
        employeeRepoMock.Setup(r => r.GetActiveEmployeesByDepartmentAsync(departmentId))
            .ReturnsAsync(employees);

        var repoMock = new Mock<ISalaryHistoryRepository>();
        repoMock.Setup(r => r.GetSalaryByMonth(1, It.IsAny<DateOnly>()))
            .ReturnsAsync((Domain.Entities.SalaryHistory)null!);

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = new SalaryHistoryService(repoMock.Object,
            employeeRepoMock.Object,
            Mock.Of<IDepartmentRepository>(),
            Mock.Of<ILogger<SalaryHistoryService>>(),
            cacheMock.Object);
        
        var result = await service.ApplyDepartmentBonusAsync(departmentId, bonusPercentage);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Data!); 
        repoMock.Verify(r => r.UpdateSalaryAsync(It.IsAny<Domain.Entities.SalaryHistory>()), Times.Never);
    }



}