using Clean.Application.Dtos.VacationRecords;
using Clean.Application.Services.VacationRecord;
using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Test.VacationRecord;

public class VacationRecordCheckerTests
{
    [Fact]
    public void CheckVacation_ShouldFail_WhenStartDateIsLessThan7DaysFromToday()
    {
        // Arrange
        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            Type = VacationType.Paid,
            Status = VacationStatus.Pending
        };

        var employee = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1))
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("7 days", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldFail_WhenVacationDurationExceeds24Days()
    {
        // Arrange
        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(40)), // > 24 days
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1))
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("24 days", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldFail_WhenEmploymentIsLessThan6Months()
    {
        // Arrange
        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3))
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("6 months", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldFail_WhenOverlapsWithExistingVacation()
    {
        // Arrange
        var existingVacation = new Domain.Entities.VacationRecord
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15))
        };

        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(12)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(18)),
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            VacationRecords = new List<Domain.Entities.VacationRecord> { existingVacation }
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("overlaps", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldFail_WhenLessThan5MonthsSinceLastVacation()
    {
        // Arrange
        var lastVacation = new Domain.Entities.VacationRecord
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-4)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-4).AddDays(5))
        };

        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(12)),
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            VacationRecords = new List<Domain.Entities.VacationRecord> { lastVacation }
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("5 months", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldFail_WhenVacationBalanceIsInsufficient()
    {
        // Arrange
        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            VacationBalances = new List<VacationBalance>
            {
                new VacationBalance { TotalDaysPerYear = 20, UsedDays = 19, Year = DateTime.Today.Year }
            }
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("available", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldFail_WhenNoPayrollRecordsExist()
    {
        // Arrange
        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            VacationBalances = new List<VacationBalance>
            {
                new VacationBalance { TotalDaysPerYear = 20, UsedDays = 5, Year = DateTime.Today.Year }
            },
            PayrollRecords = new List<Domain.Entities.PayrollRecord>() // Empty
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("No payroll", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckVacation_ShouldSucceed_ForValidPaidVacation()
    {
        // Arrange
        var request = new RequestVacationDto
        {
            EmployeeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            Type = VacationType.Paid
        };

        var employee = new Employee
        {
            Id = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            VacationBalances = new List<VacationBalance>
            {
                new VacationBalance { TotalDaysPerYear = 20, UsedDays = 5, Year = DateTime.Today.Year }
            },
            PayrollRecords = new List<Domain.Entities.PayrollRecord>
            {
                new Domain.Entities.PayrollRecord
                {
                    PeriodStart = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
                    PeriodEnd = DateOnly.FromDateTime(DateTime.Today), 
                    GrossPay = 1000m
                }
            }
        };

        // Act
        var result = VacationRecordChecker.CheckVacation(request, employee);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Contains("valid", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.PaymentAmount > 0);
    }
}
