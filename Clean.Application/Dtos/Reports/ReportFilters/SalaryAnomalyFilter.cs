namespace Clean.Application.Dtos.Reports.ReportFilters;

public class SalaryAnomalyFilter
{
    public string? Format { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public DateOnly? FromMonth { get; set; }
    public DateOnly? ToMonth { get; set; }
    public bool? IsReviewed { get; set; }
}