using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.Employee;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<PaginatedResponse<GetEmployeeDto>> GetEmployeesAsync(EmployeePaginationFilter filter)
    {
        var (employees, totalRecords) = await _employeeRepository.GetActiveEmployeesPaginatedAsync(filter);
        return new PaginatedResponse<GetEmployeeDto>(employees, filter.PageNumber, filter.PageSize, totalRecords);
    }

    public async Task<Response<GetEmployeeDto?>> GetEmployeeByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

        if (employee == null)
        {
            return new Response<GetEmployeeDto?>(HttpStatusCode.NotFound, "Employee not found");
        }

        var employeeDto = new GetEmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            BaseSalary = employee.SalaryHistories
                .OrderByDescending(sh => sh.Month)
                .Select(sh => sh.BaseAmount)
                .FirstOrDefault(),
            DepartmentName = employee.Department?.Name ?? "Unknown",
            HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
            Position = employee.Position,
            IsActive = employee.IsActive
        };

        return new Response<GetEmployeeDto?>(HttpStatusCode.OK, employeeDto);
    }

    public async Task<Response<GetEmployeeDto?>> GetEmployeeByUserId(int userId)
    {
        var employee = await _employeeRepository.GetEmployeeByUserId(userId);

        if (employee == null)
        {
            return new Response<GetEmployeeDto?>(HttpStatusCode.NotFound, "Employee not found");
        }

        var employeeDto = new GetEmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            BaseSalary = employee.SalaryHistories
                .OrderByDescending(sh => sh.Month)
                .Select(sh => sh.BaseAmount)
                .FirstOrDefault(),
            DepartmentName = employee.Department?.Name ?? "Unknown",
            HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
            Position = employee.Position,
            IsActive = employee.IsActive
        };

        return new Response<GetEmployeeDto?>(HttpStatusCode.OK, employeeDto);
    }

    public async Task<Response<GetEmployeeDto>> UpdateEmployeeAsync(UpdateEmployeeDto dto)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.Id);

        if (employee == null)
        {
            return new Response<GetEmployeeDto>(HttpStatusCode.NotFound, "Employee not found");
        }

        // Update fields only if provided
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
        {
            employee.FirstName = dto.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            employee.LastName = dto.LastName;
        }

        if (dto.Position.HasValue)
        {
            employee.Position = dto.Position.Value;
        }

        if (dto.HireDate.HasValue)
        {
            employee.HireDate = dto.HireDate.Value;
        }

        if (dto.IsActive.HasValue)
        {
            employee.IsActive = dto.IsActive.Value;
        }

        if (dto.DepartmentId.HasValue)
        {
            employee.DepartmentId = dto.DepartmentId.Value;
        }

        var updated = await _employeeRepository.UpdateEmployeeAsync(employee);

        if (updated == null)
        {
            return new Response<GetEmployeeDto>(HttpStatusCode.InternalServerError, "Failed to update employee");
        }

        var result = new GetEmployeeDto
        {
            Id = updated.Id,
            FirstName = updated.FirstName,
            LastName = updated.LastName,
            BaseSalary = updated.SalaryHistories
                .OrderByDescending(sh => sh.Month)
                .Select(sh => sh.BaseAmount)
                .FirstOrDefault(),
            DepartmentName = updated.Department?.Name ?? "Unknown",
            HireDate = updated.HireDate.ToString("yyyy-MM-dd"),
            Position = updated.Position,
            IsActive = updated.IsActive
        };

        return new Response<GetEmployeeDto>(HttpStatusCode.OK, "Employee updated successfully", result);
    }

    public async Task<Response<bool>> DeactivateEmployeeAsync(int id)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(id);

        if (employee == null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, "Employee not found");
        }

        if (!employee.IsActive)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, "Employee is already inactive");
        }

        employee.IsActive = false;
        var updated = await _employeeRepository.UpdateEmployeeAsync(employee);

        if (updated == null)
        {
            return new Response<bool>(HttpStatusCode.InternalServerError, "Failed to deactivate employee");
        }

        return new Response<bool>(HttpStatusCode.OK, "Employee deactivated successfully", true);
    }
}
