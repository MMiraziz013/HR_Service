using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports;
using Clean.Application.Dtos.Reports.ReportFilters;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    /// <summary>
    /// Downloads an employee report in the specified format (JSON or csv).
    /// </summary>
    /// <param name="filter">The filter criteria, including the Format parameter.</param>
    [HttpGet("employees")]
    [PermissionAuthorize(PermissionConstants.Employees.ManageAll)]
    public async Task<IActionResult> DownloadEmployeeReport([FromQuery] EmployeeReportFilter filter)
    {
        var report = await _reportsService.GenerateEmployeeReportAsync(filter);

        return File(
            fileContents: report.Content,
            contentType: report.ContentType,
            fileDownloadName: report.FileName
        );
    }
    
    [HttpGet("payrolls")]
    [PermissionAuthorize(PermissionConstants.Employees.ManageAll)]
    public async Task<IActionResult> DownloadPayrollReport([FromQuery] PayrollReportFilter filter)
    {
        var report = await _reportsService.GeneratePayrollReportAsync(filter);

        return File(
            fileContents: report.Content,
            contentType: report.ContentType,
            fileDownloadName: report.FileName
        );
    }
    
      
    [HttpGet("salaries")]
    [PermissionAuthorize(PermissionConstants.Employees.ManageAll)]
    public async Task<IActionResult> DownloadSalaryReport([FromQuery] SalaryFilter filter)
    {
        var report = await _reportsService.GenerateSalaryHistoryReportAsync(filter);

        return File(
            fileContents: report.Content,
            contentType: report.ContentType,
            fileDownloadName: report.FileName
        );
    }
    
    [HttpGet("anomalies")]
    [PermissionAuthorize(PermissionConstants.Employees.ManageAll)]
    public async Task<IActionResult> DownloadSalaryAnomalyReport([FromQuery] SalaryAnomalyFilter filter)
    {
        var report = await _reportsService.GenerateSalaryAnomalyReportAsync(filter);

        return File(
            fileContents: report.Content,
            contentType: report.ContentType,
            fileDownloadName: report.FileName
        );
    }
    
}