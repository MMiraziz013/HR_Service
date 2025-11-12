using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;
using Clean.Application.Services.PayrollRecord;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Clean.Application.Test.PayrollRecord;

public class PayrollRecordServiceTests
{
    private readonly Mock<IPayrollRecordRepository> _payrollRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<ISalaryHistoryRepository> _salaryRepoMock;
    private readonly Mock<ILogger<PayrollRecordService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IVacationRecordRepository> _vacationRepoMock;
    private readonly PayrollRecordService _service;

    public PayrollRecordServiceTests()
    {
        _payrollRepoMock = new Mock<IPayrollRecordRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _salaryRepoMock = new Mock<ISalaryHistoryRepository>();
        _loggerMock = new Mock<ILogger<PayrollRecordService>>();
        _cacheMock = new Mock<ICacheService>();
        _vacationRepoMock = new Mock<IVacationRecordRepository>();
        
        _service = new PayrollRecordService(
            _payrollRepoMock.Object,
            _employeeRepoMock.Object,
            _salaryRepoMock.Object,
            _loggerMock.Object,
            _cacheMock.Object,
            _vacationRepoMock.Object
        );
    }
    
    [Fact]
    public async Task AddPayrollRecordAsync_ReturnsNotFound_WhenEmployeeIsNull()
    {
        var payrollDto = new AddPayrollRecordDto { EmployeeId = 1 };
        _employeeRepoMock.Setup(x => x.GetEmployeeByIdAsync(1))
            .ReturnsAsync((Employee)null!);
        var result = await _service.AddPayrollRecordAsync(payrollDto);


        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("Employee not found.", result.Message);
    }
    
    
    [Fact]
    public async Task AddPayrollRecordAsync_ReturnsBadRequest_WhenSalaryNotFound()
    {
     
        var payrollDto = new AddPayrollRecordDto { EmployeeId = 1 };
        var employee = new Employee { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true, HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)) };

        _employeeRepoMock.Setup(x => x.GetEmployeeByIdAsync(1))
            .ReturnsAsync(employee);

        _salaryRepoMock.Setup(x => x.GetLatestSalaryHistoryAsync(1))
            .ReturnsAsync((Domain.Entities.SalaryHistory)null!);
        
        var result = await _service.AddPayrollRecordAsync(payrollDto);
        
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("No salary record found", result.Message);
    }

    [Fact]
    public async Task AddPayrollRecordAsync_ReturnsOk_WhenPayrollAddedSuccessfully()
    {
        var payrollDto = new AddPayrollRecordDto { EmployeeId = 1 };
        var today = DateTime.UtcNow;
        var workMonth = today.AddMonths(-1);

        var employee = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            HireDate = DateOnly.FromDateTime(today.AddMonths(-3))
        };

        var salary = new Domain.Entities.SalaryHistory
        {
            EmployeeId = 1,
            Month = new DateOnly(workMonth.Year, workMonth.Month, 1),
            BaseAmount = 1000
        };

        _employeeRepoMock.Setup(x => x.GetEmployeeByIdAsync(1))
            .ReturnsAsync(employee);
      
        _salaryRepoMock.Setup(x => x.GetLatestSalaryHistoryAsync(1))
            .ReturnsAsync(salary);
      
        _payrollRepoMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.PayrollRecord>()))
            .ReturnsAsync(true);
        
        _cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        
        var result = await _service.AddPayrollRecordAsync(payrollDto);
        
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("generated automatically", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(employee.Id, result.Data.EmployeeId);
        Assert.Equal(employee.FirstName + " " + employee.LastName, result.Data.EmployeeName);
    }
    
    [Fact]
    public async Task GetLatestPayrollRecordByEmployeeIdAsync_ReturnsFromCache_IfExists()
    {
        int employeeId = 1;
        var cachedResponse = new Response<GetPayrollWithSalaryDto>(
            HttpStatusCode.OK,
            "Cached",
            new GetPayrollWithSalaryDto { Id = 123 }
        );

        _cacheMock.Setup(x => x.GetAsync<Response<GetPayrollWithSalaryDto>>($"payroll_latest_employee_{employeeId}"))
            .ReturnsAsync(cachedResponse);
        
        var result = await _service.GetLatestPayrollRecordByEmployeeIdAsync(employeeId);
        
        Assert.Equal(cachedResponse, result);
        _employeeRepoMock.Verify(x => x.GetEmployeeByIdAsync(It.IsAny<int>()), Times.Never);
        _payrollRepoMock.Verify(x => x.GetLatestByEmployeeIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetLatestPayrollRecordByEmployeeIdAsync_ReturnsNotFound_WhenEmployeeNotExists()
    {
        int employeeId = 1;
        _cacheMock.Setup(x => x.GetAsync<Response<GetPayrollWithSalaryDto>>(It.IsAny<string>()))
            .ReturnsAsync((Response<GetPayrollWithSalaryDto>)null!);

        _employeeRepoMock.Setup(x => x.GetEmployeeByIdAsync(employeeId))
            .ReturnsAsync((Employee)null!);
        
        var result = await _service.GetLatestPayrollRecordByEmployeeIdAsync(employeeId);
        
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        Assert.Contains("Employee with ID", result.Message);
    }
    
    [Fact]
    public async Task GetLatestPayrollRecordByEmployeeIdAsync_ReturnsOk_WithVacationPay()
    {
   
        int employeeId = 1;
        var employee = new Employee { Id = 1, FirstName = "John", LastName = "Doe" };
        var payrollRecord = new Domain.Entities.PayrollRecord
        {
            Id = 101,
            EmployeeId = employeeId,
            PeriodStart = new DateOnly(2025, 10, 1),
            PeriodEnd = new DateOnly(2025, 10, 31),
            GrossPay = 2000,
            Deductions = 50,
            CreatedAt = DateTime.UtcNow
        };

        var salaryHistory = new Domain.Entities.SalaryHistory
        {
            BaseAmount = 1800
        };

        var vacationRecords = new List<VacationRecord>
        {
            new VacationRecord
            {
                EmployeeId = employeeId,
                Type = VacationType.Paid,
                StartDate = new DateOnly(2025, 10, 5),
                PaymentAmount = 100
            }
        };

        _cacheMock.Setup(x => x.GetAsync<Response<GetPayrollWithSalaryDto>>(It.IsAny<string>()))
            .ReturnsAsync((Response<GetPayrollWithSalaryDto>)null!);

        _employeeRepoMock.Setup(x => x.GetEmployeeByIdAsync(employeeId))
            .ReturnsAsync(employee);

        _payrollRepoMock.Setup(x => x.GetLatestByEmployeeIdAsync(employeeId))
            .ReturnsAsync(payrollRecord);

        _salaryRepoMock.Setup(x => x.GetSalaryByMonth(employeeId, payrollRecord.PeriodStart))
            .ReturnsAsync(salaryHistory);

        _vacationRepoMock.Setup(x => x.GetByEmployeeId(employeeId))
            .ReturnsAsync(vacationRecords);

        _cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

       
        var result = await _service.GetLatestPayrollRecordByEmployeeIdAsync(employeeId);

      
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(payrollRecord.Id, result.Data!.Id);
        Assert.Equal(2050, result.Data.NetPay); // NetPay + vacation pay
        Assert.Equal(100, result.Data.VacationPay);

        _cacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetPayrollForLastSixMonthAsync_ReturnsFromCache_IfExists()
    {
      
        var cachedData = new Response<List<MonthPayrollDto>>(
            HttpStatusCode.OK,
            new List<MonthPayrollDto> { new MonthPayrollDto { Month = "Jan 2025", TotalNetPay = 1000 } }
        );

        _cacheMock.Setup(x => x.GetAsync<Response<List<MonthPayrollDto>>>("payroll_last_six_months"))
            .ReturnsAsync(cachedData);
        
        var result = await _service.GetPayrollForLastSixMonthAsync();
        
        Assert.Equal(cachedData, result);
        _payrollRepoMock.Verify(x => x.GetTotalPaidForMonth(It.IsAny<DateOnly>()), Times.Never);
    }

    

}