using System.Net;
using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationRecords;
using Clean.Application.Security.Permission;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/vacation")]
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
    public async Task<IActionResult> GetVacationRecordByEmployeeIdAsync([FromQuery] VacationRecordPaginationFilter filter)
    {
        var response = await _vacationRecordService.GetVacationRecordsAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("my_records")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.View)]
    public async Task<IActionResult> GetMyVacationRecordsAsync([FromQuery] VacationRecordPaginationFilter filter)
    {
        filter.EmployeeId = int.Parse(User.FindFirstValue("EmployeeId")!);
        var response = await _vacationRecordService.GetVacationRecordsAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    // [HttpPut]
    // [PermissionAuthorize(PermissionConstants.VacationRecords.ManageSelf)]
    // public async Task<IActionResult> UpdateVacationRecordAsync([FromBody] UpdateVacationRecordDto dto)
    // {
    //     var response = await _vacationRecordService.UpdateVacationRecordAsync(dto);
    //     return StatusCode(response.StatusCode, response);    
    // }

    [HttpDelete]
    [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    public async Task<IActionResult> DeleteVacationRecordAsync(int id)
    {
        var response = await _vacationRecordService.DeleteVacationRecordAsync(id);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpPost("send-request")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.ManageSelf)]
    public async Task<IActionResult> SendVacationRequestAsync([FromBody] AddVacationRecordDto dto)
    {
        var response = await _vacationRecordService.SubmitNewVacationRequestAsync(dto);
        return StatusCode(response.StatusCode, response);
    }
    

    [HttpGet("approve")]
    public async Task<IActionResult> EmailApproveAsync([FromQuery] int id)
    {
        // The browser always sends the ID via a query string parameter.
        var dto = new VacationRecordHrResponseDto
        {
            Id = id,
            UpdatedStatus = VacationStatus.Approved
            // Comment can be null here
        };

        var response = await _vacationRecordService.HrRespondToVacationRequest(dto);

        // IMPORTANT: Return an HTML response the user can see in their browser
        if (response.StatusCode == 200)
        {
            return Content($"<h1>✅ Vacation Request ID {id} Approved!</h1><p>The system has processed your approval.</p>", "text/html");
        }
    
        // Use the status code from the service response (e.g., 400 Bad Request)
        var contentResult =  Content($"<h1>❌ Error Approving Request ID {id}</h1><p>{response.Message}</p>", "text/html");
        
        return StatusCode(response.StatusCode, contentResult);

    }


    [HttpGet("reject")]
    public async Task<IActionResult> EmailRejectAsync([FromQuery] int id)
    {
        var dto = new VacationRecordHrResponseDto
        {
            Id = id,
            UpdatedStatus = VacationStatus.Rejected,
            Comment = "Rejected via email link." // Optional comment
        };
    
        var response = await _vacationRecordService.HrRespondToVacationRequest(dto);

        if (response.StatusCode == 200)
        {
            return Content($"<h1>🚫 Vacation Request ID {id} Rejected.</h1><p>The system has processed your rejection.</p>", "text/html");
        }
        
        var contentResult = Content($"<h1>❌ Error Rejecting Request ID {id}</h1><p>{response.Message}</p>", "text/html");

        return StatusCode(response.StatusCode, contentResult);

    }

    // [HttpPut("approve")]
    // [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    // public async Task<IActionResult> ApproveAsync(VacationRecordHrResponseDto dto)
    // {
    //     dto.UpdatedStatus = VacationStatus.Approved;
    //     var response = await _vacationRecordService.HrRespondToVacationRequest(dto);
    //     return StatusCode(response.StatusCode, response);
    // }
    //
    // [HttpPut("reject")]
    // [PermissionAuthorize(PermissionConstants.VacationRecords.Manage)]
    // public async Task<IActionResult> RejectAsync(VacationRecordHrResponseDto dto)
    // {
    //     dto.UpdatedStatus = VacationStatus.Rejected;
    //     var response = await _vacationRecordService.HrRespondToVacationRequest(dto);
    //     return StatusCode(response.StatusCode, response);
    // }

    [HttpPut("cancel/{id:int}")]
    [PermissionAuthorize(PermissionConstants.VacationRecords.ManageSelf)]
    public async Task<IActionResult> CancelMyRequestAsync(int id)
    {
        var response = await _vacationRecordService.CancelVacationRequestAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}