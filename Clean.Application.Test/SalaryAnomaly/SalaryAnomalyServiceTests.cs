using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryAnomaly;
using Clean.Application.Services.SalaryAnomaly;
using Clean.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Clean.Application.Test.SalaryAnomaly;

public class SalaryAnomalyServiceTests
{
    private readonly Mock<ISalaryAnomalyRepository> _repositoryMock;
    private readonly Mock<IPayrollRecordRepository> _payrollRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<ILogger<SalaryAnomalyService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly SalaryAnomalyService _service;

    public SalaryAnomalyServiceTests()
    {
        _repositoryMock = new Mock<ISalaryAnomalyRepository>();
        _payrollRepoMock = new Mock<IPayrollRecordRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _loggerMock = new Mock<ILogger<SalaryAnomalyService>>();
        _cacheMock = new Mock<ICacheService>();
        
        _service = new SalaryAnomalyService(
            _repositoryMock.Object,
            _payrollRepoMock.Object,
            _employeeRepoMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsCached_WhenCacheExists()
    {
        var cachedResponse = new PaginatedResponse<GetSalaryAnomalyDto>(
            new List<GetSalaryAnomalyDto> { new GetSalaryAnomalyDto { Id = 1 } }, 1, 1, 1);
        
        _cacheMock.Setup(x => x.GetAsync<PaginatedResponse<GetSalaryAnomalyDto>>("salary_anomalies_all"))
            .ReturnsAsync(cachedResponse);
        
        var result = await _service.GetAllAsync();
        
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Data!);
        _repositoryMock.Verify(x => x.GetAllAsync(), Times.Never); // repository should not be called
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsNotFound_WhenNoAnomalies()
    {
        _cacheMock.Setup(x => x.GetAsync<PaginatedResponse<GetSalaryAnomalyDto>>("salary_anomalies_all"))
            .ReturnsAsync((PaginatedResponse<GetSalaryAnomalyDto>)null!);
        _repositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Domain.Entities.SalaryAnomaly>());
        
        var result = await _service.GetAllAsync();
        
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        Assert.Empty(result.Data);
    }
    
    [Fact]
    public async Task AddReviewCommentAsync_ReturnsNotFound_WhenAnomalyDoesNotExist()
    {

        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Domain.Entities.SalaryAnomaly)null!);
        
        var result = await _service.AddReviewCommentAsync(1, "Looks fine");
        
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("Anomaly is not found.", result.Message);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.SalaryAnomaly>()), Times.Never);
    }
    
    [Fact]
    public async Task AddReviewCommentAsync_ReturnsBadRequest_WhenNotReviewedYet()
    {
        var anomaly = new Domain.Entities.SalaryAnomaly { Id = 1, IsReviewed = false };
        _repositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(anomaly);
        
        var result = await _service.AddReviewCommentAsync(1, "Test comment");
        
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Unreviewed anomaly cannot be commented!", result.Message);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.SalaryAnomaly>()), Times.Never);
    }
    
    [Fact]
    public async Task GetSalaryAnomaliesForListAsync_ReturnsFromCache_WhenExists()
    {
        var cachedResponse = new PaginatedResponse<SalaryAnomalyListDto>(
            new List<SalaryAnomalyListDto>
            {
                new SalaryAnomalyListDto { Id = 1, FullName = "John Doe", Deviation = 5, IsViewed = false }
            },
            1, 5, 1)
        {
            StatusCode = (int)HttpStatusCode.OK
        };

        _cacheMock.Setup(x => x.GetAsync<PaginatedResponse<SalaryAnomalyListDto>>("salary_anomalies_list_for_graphs"))
            .ReturnsAsync(cachedResponse);
        
        var result = await _service.GetSalaryAnomaliesForListAsync();
        
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Data);
        _repositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
    }
    
    [Fact]
    public async Task GetSalaryAnomaliesForListAsync_ReturnsNotFound_WhenNoData()
    {
        _cacheMock.Setup(x => x.GetAsync<PaginatedResponse<SalaryAnomalyListDto>>("salary_anomalies_list_for_graphs"))
            .ReturnsAsync((PaginatedResponse<SalaryAnomalyListDto>)null!);

        _repositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Domain.Entities.SalaryAnomaly>());
        
        var result = await _service.GetSalaryAnomaliesForListAsync();
        
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("No salary anomalies are found", result.Message);
        Assert.Empty(result.Data);
        _cacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }
    
    [Fact]
    public async Task GetSalaryAnomaliesForListAsync_ReturnsMappedResult_WhenDataExists()
    {
        _cacheMock.Setup(x => x.GetAsync<PaginatedResponse<SalaryAnomalyListDto>>("salary_anomalies_list_for_graphs"))
            .ReturnsAsync((PaginatedResponse<SalaryAnomalyListDto>)null!);

        var anomalies = new List<Domain.Entities.SalaryAnomaly>
        {
            new Domain.Entities.SalaryAnomaly
            {
                Id = 1,
                DeviationPercent = 10,
                IsReviewed = false,
                Month = new DateOnly(2025, 10, 1),
                Employee = new Employee { FirstName = "Jane", LastName = "Smith" }
            },
            new Domain.Entities.SalaryAnomaly
            {
                Id = 2,
                DeviationPercent = 8,
                IsReviewed = true,
                Month = new DateOnly(2025, 9, 1),
                Employee = new Employee { FirstName = "John", LastName = "Doe" }
            }
        };

        _repositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(anomalies);
        _cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        
        var result = await _service.GetSalaryAnomaliesForListAsync();
        
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Salary anomalies retrieved successfully.", result.Message);
        Assert.Equal(2, result.TotalRecords);
        Assert.True(result.Data.Any(x => x.FullName == "Jane Smith"));
        Assert.True(result.Data.Any(x => x.FullName == "John Doe"));

        _cacheMock.Verify(x => x.SetAsync("salary_anomalies_list_for_graphs", It.IsAny<object>(), TimeSpan.FromMinutes(10)), Times.Once);
    }

}