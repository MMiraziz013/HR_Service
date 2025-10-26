using Clean.Application.Abstractions;
using Clean.Application.Dtos.Department;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentController : Controller
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpPost("add-department")]
    [PermissionAuthorize(PermissionConstants.Departments.Manage)]
    public async Task<IActionResult> AddDepartmentAsync(AddDepartmentDto dto)
    {
        var response = await _departmentService.AddDepartmentAsync(dto);
        return Ok(response);
    }
}