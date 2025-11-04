using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [PermissionAuthorize(PermissionConstants.Employees.View)]
    public async Task<IActionResult> GetAllAsync([FromQuery] EmployeePaginationFilter filter)
    {
        var response = await _employeeService.GetEmployeesAsync(filter);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpGet("{id:int}")]
    [PermissionAuthorize(PermissionConstants.Employees.View)]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var response = await _employeeService.GetEmployeeByIdAsync(id);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpPut("{id:int}")]
    [PermissionAuthorize(PermissionConstants.Employees.Manage)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateEmployeeDto dto)
    {
        dto.Id = id;
        var response = await _employeeService.UpdateEmployeeAsync(dto);
        return StatusCode(response.StatusCode, response);    
    }

    [HttpDelete("{id:int}")]
    [PermissionAuthorize(PermissionConstants.Employees.Manage)]
    public async Task<IActionResult> DeactivateAsync(int id)
    {
        var response = await _employeeService.DeactivateEmployeeAsync(id);
        return StatusCode(response.StatusCode, response);    
    }
}