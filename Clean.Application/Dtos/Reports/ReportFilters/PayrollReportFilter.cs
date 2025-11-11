namespace Clean.Application.Dtos.Reports.ReportFilters;

public class PayrollReportFilter
{
        // The report format requested (e.g., "json", "csv"). Can be null.
        public string? Format { get; set; }
        public int? EmployeeId { get; set; }
        public DateOnly? StartPeriod { get; set; }
        public DateOnly? EndPeriod { get; set; } 
        public int? DepartmentId { get; set; }
}