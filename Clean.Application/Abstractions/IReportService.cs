using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports;
using Clean.Application.Dtos.Reports.ReportFilters;

namespace Clean.Application.Abstractions;

public interface IReportsService
{
    /// <summary>
    /// Generate a report for employees in specified format. Returns a file payload (bytes).
    /// </summary>
    Task<ReportResult> GenerateEmployeeReportAsync(EmployeeReportFilter filter);

    Task<ReportResult> GenerateDepartmentReportAsync(DepartmentReportFilter filter);

}

public record ReportResult(byte[] Content, string ContentType, string FileName, long? ContentLength = null);