using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayrollRecordController : Controller
{
    private readonly IPayrollRecordService _payrollRecordService;

    public PayrollRecordController(IPayrollRecordService payrollRecordService)
    {
        _payrollRecordService = payrollRecordService;
    }

    [HttpPost("add")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> CreatePayrollRecord([FromBody] AddPayrollRecordDto dto)
    {
        var response = await _payrollRecordService.AddPayrollRecordAsync(dto);
        return Ok(response);
    }

    [HttpGet("get-all")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetAll()
    {
        var response = await _payrollRecordService.GetAllPayrollRecordsAsync();
        return Ok(response);
    }

    [HttpGet("get-by-id")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetById([FromQuery] int id)
    {
        var response = await _payrollRecordService.GetPayrollRecordByIdAsync(id);
        return Ok(response);
    }

    [HttpGet("get/{employeeId:int}")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetByEmployeeId(int employeeId)
    {
        var response = await _payrollRecordService.GetPayrollRecordsByEmployeeIdAsync(employeeId);
        return Ok(response);
    }

    [HttpGet("get")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetByEmployeeIdForHr([FromQuery]int id)
    {
        var response = await _payrollRecordService.GetPayrollRecordsByEmployeeIdAsync(id);
        return Ok(response);
    }

    [HttpGet("get-latest/{employeeId:int}")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetLatest(int employeeId)
    {
        var response = await _payrollRecordService.GetLatestPayrollRecordByEmployeeIdAsync(employeeId);
        return Ok(response);
    }

    [HttpGet("get-latest")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetLatestForHr([FromQuery] int id)
    {
        var response = await _payrollRecordService.GetLatestPayrollRecordByEmployeeIdAsync(id);
        return Ok(response);
    }

    [HttpPut("update")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> Update([FromBody] UpdatePayrollRecordDto dto)
    {
        var response = await _payrollRecordService.UpdatePayrollRecordAsync(dto);
        return Ok(response);
    }

    [HttpDelete("delete")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> Delete([FromQuery] int id)
    {
        var response = await _payrollRecordService.DeletePayrollRecordAsync(id);
        return Ok(response);
    }
}