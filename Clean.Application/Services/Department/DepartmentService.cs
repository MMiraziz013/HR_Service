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

    public async Task<Response<GetDepartmentDto>> AddDepartmentAsync(AddDepartmentDto dto)
    {
        var department = new Domain.Entities.Department
        {
            Name = dto.Name,
            Description = dto.Description
        };

        var isAdded = await _departmentRepository.AddDepartmentAsync(department);
        if (isAdded == false)
        {
            return new Response<GetDepartmentDto>(HttpStatusCode.BadRequest, message: "Error while adding the department (maybe it already exists).");
        }

        var addedDepartment = new GetDepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description
        };

        return new Response<GetDepartmentDto>(HttpStatusCode.OK, message: "Department added successfully!", addedDepartment);
    }

    public async Task<Response<List<GetDepartmentDto>>> GetDepartmentsAsync(string? search = null)
    {
        var departments = await _departmentRepository.GetDepartmentsAsync(search);

        if (departments.Count == 0)
        {
            return new Response<List<GetDepartmentDto>>(HttpStatusCode.NotFound, "No departments found.");
        }

        var departmentDtos = departments.Select(d => new GetDepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description
        }).ToList();

        return new Response<List<GetDepartmentDto>>(HttpStatusCode.OK, "Departments retrieved successfully!", departmentDtos);
    }

    public async Task<Response<List<GetDepartmentWithEmployeesDto>>> GetDepartmentsWithEmployeesAsync(string? search = null)
    {
        var departments = await _departmentRepository.GetDepartmentsAsync(search);

        if (departments.Count == 0)
        {
            return new Response<List<GetDepartmentWithEmployeesDto>>(HttpStatusCode.NotFound, "No departments found.");
        }

        var departmentDtos = departments.Select(d => new GetDepartmentWithEmployeesDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Employees = d.Employees.Select(e => new GetEmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                BaseSalary = e.SalaryHistories
                    .OrderByDescending(sh => sh.Month)
                    .Select(sh => sh.BaseAmount)
                    .FirstOrDefault(),
                DepartmentName = d.Name,
                HireDate = e.HireDate.ToString("yyyy-MM-dd"),
                Position = e.Position,
                IsActive = e.IsActive
            }).ToList()
        }).ToList();

        return new Response<List<GetDepartmentWithEmployeesDto>>(HttpStatusCode.OK, "Departments with employees retrieved successfully!", departmentDtos);
    }

    public async Task<Response<List<GetDepartmentSummaryDto>>> GetDepartmentsSummaryAsync(string? search)
    {
        var departments = await _departmentRepository.GetDepartmentsAsync(search);
        if (departments.Count == 0)
        {
            return new Response<List<GetDepartmentSummaryDto>>(HttpStatusCode.NotFound, "No departments found.");
        }

        var departmentDtos = departments.Select(d => new GetDepartmentSummaryDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            EmployeeCount = d.Employees.Count
        }).ToList();


        return new Response<List<GetDepartmentSummaryDto>>(HttpStatusCode.OK, departmentDtos);
    }

    public async Task<Response<GetDepartmentDto?>> GetDepartmentByIdAsync(int id)
    {
        var department = await _departmentRepository.GetDepartmentByIdAsync(id);
        if (department == null)
        {
            return new Response<GetDepartmentDto?>(HttpStatusCode.NotFound, "Department not found.");
        }

        var dto = new GetDepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
        };

        return new Response<GetDepartmentDto?>(HttpStatusCode.OK, "Department retrieved successfully.", dto);
    }
    
    public async Task<Response<GetDepartmentWithEmployeesDto?>> GetDepartmentByIdWithEmployeesAsync(int id)
    {
        var department = await _departmentRepository.GetDepartmentByIdAsync(id);
        if (department == null)
        {
            return new Response<GetDepartmentWithEmployeesDto?>(HttpStatusCode.NotFound, "Department not found.");
        }

        var dto = new GetDepartmentWithEmployeesDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            Employees = department.Employees.Select(e => new GetEmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                BaseSalary = e.SalaryHistories
                    .OrderByDescending(sh => sh.Month)
                    .Select(sh => sh.BaseAmount)
                    .FirstOrDefault(),
                DepartmentName = department.Name,
                HireDate = e.HireDate.ToString("yyyy-MM-dd"),
                Position = e.Position,
                IsActive = e.IsActive
            }).ToList()
        };

        return new Response<GetDepartmentWithEmployeesDto?>(HttpStatusCode.OK, "Department retrieved successfully.", dto);
    }

    public async Task<Response<GetDepartmentDto>> UpdateDepartmentAsync(UpdateDepartmentDto dto)
    {
        var existing = await _departmentRepository.GetDepartmentByIdAsync(dto.Id);
        if (existing == null)
        {
            return new Response<GetDepartmentDto>(HttpStatusCode.NotFound, "Department not found.");
        }

        if (string.IsNullOrEmpty(dto.Name) == false)
        {
            existing.Name = dto.Name;
        }

        if (string.IsNullOrEmpty(dto.Description) == false)
        {
            existing.Description = dto.Description;
        }

        var isUpdated = await _departmentRepository.UpdateDepartmentAsync(existing);
        if (isUpdated == false)
        {
            return new Response<GetDepartmentDto>(HttpStatusCode.InternalServerError, "Failed to update department.");
        }

        var updatedDto = new GetDepartmentDto
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description
        };

        return new Response<GetDepartmentDto>(HttpStatusCode.OK, "Department updated successfully!", updatedDto);
    }

    public async Task<Response<bool>> DeleteDepartmentAsync(int id)
    {
        var isDeleted = await _departmentRepository.DeleteDepartmentAsync(id);
        if (isDeleted == false)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, "Failed to delete department or department not found.");
        }

        return new Response<bool>(HttpStatusCode.OK, "Department deleted successfully!", true);
    }
}
