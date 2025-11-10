namespace Clean.Application.Dtos.Reports.ReportFilters;

public class DepartmentReportFilter
{
    public string? Format { get; set; }
    public string? Name { get; set; }
    public int? MinEmployeeCount { get; set; }
}