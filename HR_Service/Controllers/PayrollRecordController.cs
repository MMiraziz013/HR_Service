using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/payroll_record")]
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
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("get-all")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetAll()
    {
        var response = await _payrollRecordService.GetAllPayrollRecordsAsync();
        return StatusCode(response.StatusCode, response);
    }
   
    [HttpGet("get-by-id")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetById([FromQuery] int id)
    {
        var response = await _payrollRecordService.GetPayrollRecordByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("get/{employeeId:int}")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetByEmployeeId(int employeeId)
    {
        var response = await _payrollRecordService.GetPayrollRecordsByEmployeeIdAsync(employeeId);
        return StatusCode(response.StatusCode, response);
    }
    

    [HttpGet("get-latest/{employeeId:int}")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.View)]
    public async Task<IActionResult> GetLatest(int employeeId)
    {
        var response = await _payrollRecordService.GetLatestPayrollRecordByEmployeeIdAsync(employeeId);
        return StatusCode(response.StatusCode, response);
    }
    

    [HttpPut("update")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> Update([FromBody] UpdatePayrollRecordDto dto)
    {
        var response = await _payrollRecordService.UpdatePayrollRecordAsync(dto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("statistics/total-net-pay")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> GetTotalNetPayForLastSixMonths()
    {
        var result = await _payrollRecordService.GetPayrollForLastSixMonthAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-graph")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> GetExpextedAndActualAsync([FromQuery]DateTime startMonth,DateTime endMonth)
    {
        var response = await _payrollRecordService.GetPayrollSummaryAsync(startMonth, endMonth);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("delete")]
    [PermissionAuthorize(PermissionConstants.PayrollRecords.Manage)]
    public async Task<IActionResult> Delete([FromQuery] int id)
    {
        var response = await _payrollRecordService.DeletePayrollRecordAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}