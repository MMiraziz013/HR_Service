using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Reports.ReportFilters;

public class VacationBalanceReportFilter
{

    public string Format { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? DepartmentName { get; set; }
    public string? ByEmployeeName { get; set; }

    public int? MinWorkedYears { get; set; }
    
    public int? MaxWorkedYears { get; set; }
    
    public int? EmployeeId { get; set; }
    
    public UserRole? Role { get; set; }
    public EmployeePosition? Position { get; set; }

    public DateOnly? FromPeriodStart { get; set; }
    public DateOnly? ToPeriodEnd { get; set; }
    
    public bool? HasBonusDays { get; set; }
    
    public int? MinRemainingDays { get; set; }
    
    public int? MaxRemainingDays { get; set; }

    public int? MinUsedDays { get; set; }
    
    public int? MaxUsedDays { get; set; }
    
    public bool? HasUsedDays { get; set; }
    public bool? IsLimitFinished { get; set; }
}