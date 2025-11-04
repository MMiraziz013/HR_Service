using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationBalance;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/vacation_balances")]
public class VacationBalanceController : Controller
{
    private readonly IVacationBalanceService _vacationBalanceService;

    public VacationBalanceController(IVacationBalanceService vacationBalanceService)
    {
        _vacationBalanceService = vacationBalanceService;
    }

    [HttpPost("add")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> AddVacationBalance([FromBody] AddVacationBalanceDto dto)
    {
        var response = await _vacationBalanceService.AddVacationBalanceAsync(dto);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("all")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetAllVacationBalances([FromQuery] VacationBalanceFilter filter)
    {
        var response = await _vacationBalanceService.GetAllVacationBalancesAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("latest")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetLatestVacationBalances([FromQuery] VacationBalanceFilter filter)
    {
        var response = await _vacationBalanceService.GetLatestVacationBalancesAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("{id:int}")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetVacationBalanceById(int id)
    {
        var response = await _vacationBalanceService.GetVacationBalanceByIdAsync(id);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("by-employee/{employeeId:int}")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetVacationBalanceByEmployeeId(int employeeId)
    {
        var response = await _vacationBalanceService.GetVacationBalanceByEmployeeIdAsync(employeeId);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("me")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.ManageSelf)]
    public async Task<IActionResult> GetMyVacationBalanceAsync()
    {
        var thisEmployeeId = User.FindFirstValue("EmployeeId");
        var response = await _vacationBalanceService.GetVacationBalanceByEmployeeIdAsync(int.Parse(thisEmployeeId!));
        return StatusCode(response.StatusCode, response);    
    }

    [HttpPut("update")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> UpdateVacationBalance([FromBody] UpdateVacationBalanceDto dto)
    {
        var response = await _vacationBalanceService.UpdateVacationBalanceAsync(dto);
        return StatusCode(response.StatusCode, response);    
    }

    // [HttpDelete("delete")]
    // [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    // public async Task<IActionResult> DeleteVacationBalance([FromQuery] int vacationBalanceId)
    // {
    //     var response = await _vacationBalanceService.DeleteVacationBalanceAsync(vacationBalanceId);
    //     return Ok(response);
    // }
}