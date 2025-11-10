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
    public async Task<IActionResult> DownloadEmployeeReportAsync([FromQuery] EmployeeReportFilter filter)
    {
        var report = await _reportsService.GenerateEmployeeReportAsync(filter);

        return File(
            fileContents: report.Content,
            contentType: report.ContentType,
            fileDownloadName: report.FileName
        );
    }

    [HttpGet("departments")]
    [PermissionAuthorize(PermissionConstants.Departments.Manage)]
    public async Task<IActionResult> DownloadDepartmentReportAsync([FromQuery] DepartmentReportFilter filter)
    {
        var report = await _reportsService.GenerateDepartmentReportAsync(filter);

        return File(
            fileContents: report.Content,
            contentType: report.ContentType,
            fileDownloadName: report.FileName
        );
    }
}