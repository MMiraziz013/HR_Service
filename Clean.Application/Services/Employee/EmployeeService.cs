using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.Employee;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICacheService _redisCache;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository employeeRepository, ICacheService redisCache, ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _redisCache = redisCache;
        _logger = logger;
    }

    public async Task<PaginatedResponse<GetEmployeeDto>> GetEmployeesAsync(EmployeePaginationFilter filter)
    {
        try
        {
            var cacheKey = GetEmployeesCacheKey(filter);

            var cached = await _redisCache.GetAsync<PaginatedResponse<GetEmployeeDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var (employees, totalRecords) = await _employeeRepository.GetActiveEmployeesPaginatedAsync(filter);
            var response =
                new PaginatedResponse<GetEmployeeDto>(employees, filter.PageNumber, filter.PageSize, totalRecords);

            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching employees with filter: {@Filter}", filter);
            return new PaginatedResponse<GetEmployeeDto>(new List<GetEmployeeDto>(), filter.PageNumber, filter.PageSize,
                0);
        }
    }



    public async Task<Response<GetEmployeeDto?>> GetEmployeeByIdAsync(int id)
    {
        try
        {
            var cacheKey = $"employee_{id}";

            var cachedEmployee = await _redisCache.GetAsync<GetEmployeeDto>(cacheKey);
            if (cachedEmployee != null)
            {
                return new Response<GetEmployeeDto?>(HttpStatusCode.OK, cachedEmployee);
            }

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

            await _redisCache.SetAsync(cacheKey, employeeDto, TimeSpan.FromMinutes(15));

            return new Response<GetEmployeeDto?>(HttpStatusCode.OK, employeeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching employee by ID {EmployeeId}", id);
            return new Response<GetEmployeeDto?>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving employee.");
        }
    }


    public async Task<Response<GetEmployeeDto?>> GetEmployeeByUserId(int userId)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching employee by User ID {UserId}", userId);
            return new Response<GetEmployeeDto?>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving employee.");
        }
    }

    public async Task<Response<GetEmployeeDto>> UpdateEmployeeAsync(UpdateEmployeeDto dto)
    {
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.Id);

            if (employee == null)
            {
                return new Response<GetEmployeeDto>(HttpStatusCode.NotFound, "Employee not found");
            }

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                employee.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                employee.LastName = dto.LastName;

            if (dto.Position.HasValue)
                employee.Position = dto.Position.Value;

            if (dto.HireDate.HasValue)
                employee.HireDate = dto.HireDate.Value;

            if (dto.IsActive.HasValue)
                employee.IsActive = dto.IsActive.Value;

            if (dto.DepartmentId.HasValue)
                employee.DepartmentId = dto.DepartmentId.Value;

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

            await _redisCache.RemoveByPatternAsync("employees_*");
            await _redisCache.RemoveAsync($"employee_{employee.Id}");
            await _redisCache.RemoveAsync($"department_with_employees_{employee.DepartmentId}");
            await _redisCache.RemoveByPatternAsync("departments_with_employees_*");

            return new Response<GetEmployeeDto>(HttpStatusCode.OK, "Employee updated successfully", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating employee {@Employee}", dto);
            return new Response<GetEmployeeDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while updating employee.");
        }
    }

    public async Task<Response<bool>> DeactivateEmployeeAsync(int id)
    {
        try
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

            await _redisCache.RemoveByPatternAsync("employees_*");
            await _redisCache.RemoveAsync($"employee_{employee.Id}");
            await _redisCache.RemoveAsync($"department_with_employees_{employee.DepartmentId}");
            await _redisCache.RemoveByPatternAsync("departments_with_employees_*");

            return new Response<bool>(HttpStatusCode.OK, "Employee deactivated successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deactivating employee with ID {EmployeeId}", id);
            return new Response<bool>(HttpStatusCode.InternalServerError, "An unexpected error occurred while deactivating employee.");
        }
    }
    
    private static string GetEmployeesCacheKey(EmployeePaginationFilter filter)
    {
        // Concatenate all filter values into a single string
        return $"employees_" +
               $"page:{filter.PageNumber}_size:{filter.PageSize}_" +
               $"email:{filter.Email ?? "null"}_" +
               $"first:{filter.FirstName ?? "null"}_" +
               $"last:{filter.LastName ?? "null"}_" +
               $"position:{filter.Position?.ToString() ?? "null"}_" +
               $"dept:{filter.DepartmentName ?? "null"}_" +
               $"active:{filter.IsActive?.ToString() ?? "null"}";
    }
}
