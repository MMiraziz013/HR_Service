namespace Clean.Application.Dtos.Reports.ReportFilters;

public class EmployeeReportFilter
{
    // The report format requested (e.g., "json", "csv"). Can be null.
    public string? Format { get; set; }

    // Filtering criteria for employees
    public DateTime? HiredAfter { get; set; }
    public DateTime? HiredBefore { get; set; }

    public int? DepartmentId { get; set; }

    // Add other relevant filters here (e.g., DepartmentId, IsActive, etc.)
    // public int? DepartmentId { get; set; }
}