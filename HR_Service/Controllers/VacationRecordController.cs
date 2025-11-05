using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationRecords;
using Clean.Application.Security.Permission;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/vacation_records")]
public class VacationRecordController : Controller
{
    private readonly IVacationRecordService _vacationRecordService;

    public VacationRecordController(IVacationRecordService vacationRecordService)
    {
        _vacationRecordService = vacationRecordService;
    }

    [HttpPost]
    [PermissionAuthorize(PermissionConstants.VacationRecords.ManageSelf)]
    public async Task<IActionResult> AddVacationRecordAsync(AddVacationRecordDto dto)
    {
        var response = await _vacationRecordService.AddVacationRecordAsync(dto);
        return StatusCode(response.StatusCode, response);    
    }
    
    [HttpGet("get_all")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    public async Task<IActionResult> GetAllVacationRecordsAsync([FromQuery] VacationRecordPaginationFilter filter)
    {
        var response = await _vacationRecordService.GetVacationRecordsAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("summary")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.View)]
    public async Task<IActionResult> GetVacationRecordsSummary()
    {
        var response = await _vacationRecordService.GetVacationSummaryForLastFiveMonthsAsync();
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("{id:int}")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.ManageSelf)]
    public async Task<IActionResult> GetVacationRecordByIdAsync(int id)
    {
        var response = await _vacationRecordService.GetVacationRecordByIdAsync(id);
        return StatusCode(response.StatusCode, response);    
    }
    
    [HttpGet("by-employee/")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    public async Task<IActionResult> GetVacationRecordByEmployeeIdAsync([FromBody] VacationRecordPaginationFilter filter)
    {
        var response = await _vacationRecordService.GetVacationRecordsAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("my_records")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.View)]
    public async Task<IActionResult> GetMyVacationRecordsAsync([FromBody] VacationRecordPaginationFilter filter)
    {
        filter.EmployeeId = int.Parse(User.FindFirstValue("EmployeeId")!);
        var response = await _vacationRecordService.GetVacationRecordsAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpPut]
    [PermissionAuthorize(PermissionConstants.VacationRecords.ManageSelf)]
    public async Task<IActionResult> UpdateVacationRecordAsync([FromBody] UpdateVacationRecordDto dto)
    {
        var response = await _vacationRecordService.UpdateVacationRecordAsync(dto);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpDelete]
    [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    public async Task<IActionResult> DeleteVacationRecordAsync(int id)
    {
        var response = await _vacationRecordService.DeleteVacationRecordAsync(id);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpPut("approve")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    public async Task<IActionResult> ApproveAsync(VacationRecordHrResponseDto dto)
    {
        dto.UpdatedStatus = VacationStatus.Approved;
        var response = await _vacationRecordService.HrRespondToVacationRequest(dto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPut("reject")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    public async Task<IActionResult> RejectAsync(VacationRecordHrResponseDto dto)
    {
        dto.UpdatedStatus = VacationStatus.Rejected;
        var response = await _vacationRecordService.HrRespondToVacationRequest(dto);
        return StatusCode(response.StatusCode, response);
    }
}