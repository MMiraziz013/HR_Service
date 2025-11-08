using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.SalaryHistory;
using Clean.Application.Security.Permission;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/salary_history")]
public class SalaryHistoryController : Controller
{
  private readonly ISalaryHistoryService _salaryHistoryService;

  public SalaryHistoryController(ISalaryHistoryService salaryHistoryService)
  {
    _salaryHistoryService = salaryHistoryService;
  }

  [HttpPost("add")] 
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> CreateSalaryHistory([FromBody] AddSalaryHistoryDto salaryHistory)
  {
    var response =await _salaryHistoryService.AddSalaryHistoryAsync(salaryHistory);
    return StatusCode(response.StatusCode, response);
  }

  [HttpGet("get")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> GetAllSalaryHistories([FromQuery] SalaryHistoryFilter filter)
  {
    var response = await _salaryHistoryService.GetAllAsync(filter);
    return StatusCode(response.StatusCode, response);
  }

  [HttpGet("get-latest")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> GetLatestSalaryHistory([FromQuery] SalaryHistoryFilter filter)
  {
    var response = await _salaryHistoryService.GetLatestSalaryHistoriesAsync(filter);
    return StatusCode(response.StatusCode, response);
  }

  [HttpGet("get-by-id")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> GetSalaryHistoryById([FromQuery] int id)
  {
    var response = await _salaryHistoryService.GetByIdAsync(id);
    return StatusCode(response.StatusCode, response);
  }

  [HttpGet("get-by-employee/{employeeId:int}")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> GetSalaryHistoryByEmployeeId(int employeeId)
  {
    var response = await _salaryHistoryService.GetSalaryHistoryByEmployeeIdAsync(employeeId);
    return StatusCode(response.StatusCode, response);
  }

  [HttpPut("update/{employeeId:int}")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> UpdateBaseSalaryAsync(int employeeId,[FromBody]UpdateSalaryDto salary)
  {
    salary.EmployeeId = employeeId;
    var response = await _salaryHistoryService.UpdateSalaryHistoryAsync(salary);
    return StatusCode(response.StatusCode, response);
  }

  [HttpPut("set-bonuses")]
  [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  public async Task<IActionResult> SetBonusesAsync([FromQuery] int departmentId, [FromQuery] decimal bonusPercentage)
  {
    var response =await _salaryHistoryService.ApplyDepartmentBonusAsync(departmentId, bonusPercentage);
    return StatusCode(response.StatusCode, response);
  }
  
  //
  // [HttpGet("get-all")]
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  // public async Task<IActionResult> GetAll()
  // {
  //   var response = await _salaryHistoryService.GetAllAsync();
  //   return Ok(response);
  // }
  //
  // [HttpGet("get{employeeId:int}")]//for employee interface
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  // public async Task<IActionResult> GetSalaryHistoryByEmployeeId(int employeeId)
  // {
  //   var response =await _salaryHistoryService.GetSalaryHistoryByEmployeeIdAsync(employeeId);
  //   return Ok(response);
  // }
  //
  // [HttpGet("get")]// for hr 
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  // public async Task<IActionResult> GetEmployeeSalaryHistoryForHr([FromBody] int employeeId)
  // {
  //   var response =await _salaryHistoryService.GetSalaryHistoryByEmployeeIdAsync(employeeId);
  //   return Ok(response);
  // }
  //
  //
  //
  // [HttpGet("get-by-month/{employeeId:int}")] // for employee 
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.View)]
  // public async Task<IActionResult> GetSalaryHistoryByMonthAsync(int employeeId,  [FromQuery] DateOnly month)
  // {
  //   var response = await _salaryHistoryService.GetSalaryHistoryByMonthAsync(employeeId, month);
  //   return Ok(response);
  // }
  //
  // [HttpGet("get-monthly")]
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  // public async Task<IActionResult> GetSalaryRecordsByMonth([FromQuery] DateTime month)
  // {
  //   var response = await _salaryHistoryService.GetSalaryHistoryByMonthAsync(month);
  //   return Ok(response);
  // }
  //
  // [HttpGet("get-by-month")]
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  // public async Task<IActionResult> GetSalaryHistoryByMonthForHr([FromQuery] int employeeId, [FromQuery] DateOnly month)
  // {
  //   var response=await _salaryHistoryService.GetSalaryHistoryByMonthAsync(employeeId, month);
  //   return Ok(response);
  // }
  //
  //
  // [HttpGet("get-by-department")]
  // [PermissionAuthorize(PermissionConstants.SalaryHistories.Manage)]
  // public async Task<IActionResult> GetTotalPaidAmountByDepartment([FromQuery]int departmentId, [FromQuery] DateOnly month)
  // {
  //   var response = await _salaryHistoryService.GetTotalPaidAmountByDepartmentAsync(departmentId, month);
  //   return Ok(response);
  // }

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