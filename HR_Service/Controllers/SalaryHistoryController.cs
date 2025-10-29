using Clean.Application.Abstractions;
using Clean.Application.Dtos.SalaryHistory;
using Clean.Application.Security.Permission;
using Clean.Application.Services.SalaryHistory;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalaryHistoryController : Controller
{
  private readonly ISalaryHistoryService _salaryHistoryService;

  public SalaryHistoryController(ISalaryHistoryService salaryHistoryService)
  {
    _salaryHistoryService = salaryHistoryService;
  }

  [HttpPost("add")] // for hr
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> CreateSalaryHistory([FromBody] AddSalaryHistoryDto salaryHistory)
  {
    var response =await _salaryHistoryService.AddSalaryHistoryAsync(salaryHistory);
    return Ok(response);
  }

  [HttpGet("get{employeeId:int}")]//for employee interface
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetSalaryHistoryByEmployeeId(int employeeId)
  {
    var response =await _salaryHistoryService.GetSalaryHistoryByEmployeeIdAsync(employeeId);
    return Ok(response);
  }

  [HttpGet("get")]// for hr 
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetEmployeeSalaryHistoryForHr([FromQuery] int employeeId)
  {
    var response =await _salaryHistoryService.GetSalaryHistoryByEmployeeIdAsync(employeeId);
    return Ok(response);
  }

  [HttpGet("get-by-id")] // for hr
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetSalaryHistoryBySalaryId([FromQuery] int salaryId)
  {
    var response = await _salaryHistoryService.GetSalaryHistoryByIdAsync(salaryId);
    return Ok(response);
  }


  [HttpGet("get-by-month/{employeeId:int}")] // for employee 
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetSalaryHistoryByMonthAsync(int employeeId,  [FromQuery] DateOnly month)
  {
    var response = await _salaryHistoryService.GetSalaryHistoryByMonthAsync(employeeId, month);
    return Ok(response);
  }

  [HttpGet("get-monthly")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetSalaryRecordsByMonth([FromQuery] DateTime month)
  {
    var response = await _salaryHistoryService.GetSalaryHistoryByMonthAsync(month);
    return Ok(response);
  }
  [HttpGet("get-by-month")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetSalaryHistoryByMonthForHr([FromQuery] int employeeId, [FromQuery] DateOnly month)
  {
    var response=await _salaryHistoryService.GetSalaryHistoryByMonthAsync(employeeId, month);
    return Ok(response);
  }
  

  [HttpGet("get-by-department")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  public async Task<IActionResult> GetTotalPaidAmountByDepartment([FromQuery]int departmentId, [FromQuery] DateOnly month)
  {
    var response = await _salaryHistoryService.GetTotalPaidAmountByDepartmentAsync(departmentId, month);
    return Ok(response);
  }

  // [HttpDelete("delete")]
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  // public async Task<IActionResult> DeleteSalaryHistory([FromQuery] int id)
  // {
  //   var response = await _salaryHistoryService.DeleteSalaryHistoryAsync(id);
  //   return Ok(response);
  // }
  
  // [HttpGet("get-total/{employeeId}")]
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  // public async Task<IActionResult> GetTotalPaidSalaryForParticularTime(int employeeId, DateTime startDate, DateTime endDate)
  // {
  //   var response=await _salaryHistoryService.GetTotalPaidAmountAsync(employeeId, startDate, endDate);
  //   return Ok(response);
  // }
}