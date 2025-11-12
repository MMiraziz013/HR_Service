using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Reports.ReportFilters;

public class VacationRecordReportFilter
{
    public string Format { get; set; } = string.Empty;
    public int? Id { get; set; }
    public int? EmployeeId { get; set; }
    public UserRole? Role { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public int? Year { get; set; }

    public int? MinDuration { get; set; }
    public int? MaxDuration { get; set; }
    
    public decimal? MinPaymentAmount { get; set; }
    public decimal? MaxPaymentAmount { get; set; }
    public VacationType? VacationType { get; set; }
    public VacationStatus? Status { get; set; }

    public bool? IsCurrentlyActive { get; set; }

    public DateOnly? StartDateFrom { get; set; }
    public DateOnly? StartDateTo { get; set; }
    
    public DateOnly? EndDateFrom { get; set; }
    public DateOnly? EndDateTo { get; set; }
}