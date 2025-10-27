using Clean.Application.Abstractions;
using Clean.Application.Dtos.Department;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpPost]
    [PermissionAuthorize(PermissionConstants.Departments.Manage)]
    public async Task<IActionResult> CreateAsync([FromBody] AddDepartmentDto dto)
    {
        var response = await _departmentService.AddDepartmentAsync(dto);
        return Ok(response);
    }

    [HttpGet]
    [PermissionAuthorize(PermissionConstants.Departments.View)]
    public async Task<IActionResult> GetAllAsync([FromQuery] string? search = null)
    {
        var response = await _departmentService.GetDepartmentsAsync(search);
        return Ok(response);
    }

    [HttpGet("with-employees")]
    [PermissionAuthorize(PermissionConstants.Departments.View)]
    public async Task<IActionResult> GetAllWithEmployeesAsync([FromQuery] string? search = null)
    {
        var response = await _departmentService.GetDepartmentsWithEmployeesAsync(search);
        return Ok(response);
    }


    // ✅ Get a department by ID
    [HttpGet("{id:int}")]
    [PermissionAuthorize(PermissionConstants.Departments.View)]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var response = await _departmentService.GetDepartmentByIdAsync(id);
        return Ok(response);
    }
    
    // ✅ Get a department by ID
    [HttpGet("with-employees/{id:int}")]
    [PermissionAuthorize(PermissionConstants.Departments.View)]
    public async Task<IActionResult> GetByIdWithEmployeesAsync(int id)
    {
        var response = await _departmentService.GetDepartmentByIdWithEmployeesAsync(id);
        return Ok(response);
    }

    // ✅ Update department info
    [HttpPut("{id:int}")]
    [PermissionAuthorize(PermissionConstants.Departments.Manage)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateDepartmentDto dto)
    {
        dto.Id = id;
        var response = await _departmentService.UpdateDepartmentAsync(dto);
        return Ok(response);
    }

    // ✅ Delete department
    [HttpDelete("{id:int}")]
    [PermissionAuthorize(PermissionConstants.Departments.Manage)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var response = await _departmentService.DeleteDepartmentAsync(id);
        return Ok(response);
    }
}