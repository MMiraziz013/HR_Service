using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Department;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.Department;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }
    
    public async Task<Response<bool>> AddDepartmentAsync(AddDepartmentDto dto)
    {
        var department = new Domain.Entities.Department
        {
            Name = dto.Name,
            Description = dto.Description
        };

        var isAdded = await _departmentRepository.AddDepartmentAsync(department);
        if (isAdded == false)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, message:"Error while adding the department");
        }

        return new Response<bool>(HttpStatusCode.OK, message: "Department added!", isAdded);
    }

    public async Task<Response<List<GetDepartmentDto>>> GetDepartmentsAsync()
    {
        var departments = await _departmentRepository.GetDepartmentsAsync();
        var departmentDtos = departments.Select(d => new GetDepartmentDto
        {
            Name = d.Name,
            Description = d.Description
        }).ToList();

        return new Response<List<GetDepartmentDto>>(HttpStatusCode.OK, departmentDtos);
    }

    public async Task<Response<List<GetDepartmentWithEmployeesDto>>> GetDepartmentsWithEmployeesAsync()
    {
        var departments = await _departmentRepository.GetDepartmentsAsync();
        var departmentDtos = departments.Select(d => new GetDepartmentWithEmployeesDto()
        {
            Name = d.Name,
            Description = d.Description,
            Employees = d.Employees.Select(e=> new GetEmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                BaseSalary = e.BaseSalary,
                DepartmentName = d.Name,
                HireDate = e.HireDate.ToString("yyyy-MM-dd"),
                Position = e.Position,
                IsActive = e.IsActive
            }).ToList()
        }).ToList();

        return new Response<List<GetDepartmentWithEmployeesDto>>(HttpStatusCode.OK, departmentDtos);
    }
}