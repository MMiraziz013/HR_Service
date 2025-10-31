using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationBalance;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        return Ok(response);
    }

    [HttpGet("get-all")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetAllVacationBalances([FromQuery] VacationBalanceFilter filter)
    {
        var response = await _vacationBalanceService.GetAllVacationBalancesAsync(filter);
        return Ok(response);
    }

    [HttpGet("get-latest")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetLatestVacationBalances([FromQuery] VacationBalanceFilter filter)
    {
        var response = await _vacationBalanceService.GetLatestVacationBalancesAsync(filter);
        return Ok(response);
    }

    [HttpGet("get-by-id")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetVacationBalanceById([FromQuery] int id)
    {
        var response = await _vacationBalanceService.GetVacationBalanceByIdAsync(id);
        return Ok(response);
    }

    [HttpGet("get-by-employee/{employeeId:int}")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> GetVacationBalanceByEmployeeId(int employeeId)
    {
        var response = await _vacationBalanceService.GetVacationBalanceByEmployeeIdAsync(employeeId);
        return Ok(response);
    }

    [HttpPut("update")]
    [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    public async Task<IActionResult> UpdateVacationBalance([FromBody] UpdateVacationBalanceDto dto)
    {
        var response = await _vacationBalanceService.UpdateVacationBalanceAsync(dto);
        return Ok(response);
    }

    // [HttpDelete("delete")]
    // [PermissionAuthorize(PermissionConstants.VacationBalance.Manage)]
    // public async Task<IActionResult> DeleteVacationBalance([FromQuery] int vacationBalanceId)
    // {
    //     var response = await _vacationBalanceService.DeleteVacationBalanceAsync(vacationBalanceId);
    //     return Ok(response);
    // }
}