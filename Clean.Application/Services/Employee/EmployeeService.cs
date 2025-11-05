using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.Employee;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICacheService _redisCache;

    public EmployeeService(IEmployeeRepository employeeRepository, ICacheService redisCache)
    {
        _employeeRepository = employeeRepository;
        _redisCache = redisCache;
    }

    public async Task<PaginatedResponse<GetEmployeeDto>> GetEmployeesAsync(EmployeePaginationFilter filter)
    {
        var cacheKey = GetEmployeesCacheKey(filter);

        // Try to get from cache
        var cached = await _redisCache.GetAsync<PaginatedResponse<GetEmployeeDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var (employees, totalRecords) = await _employeeRepository.GetActiveEmployeesPaginatedAsync(filter);
        var response = new PaginatedResponse<GetEmployeeDto>(employees, filter.PageNumber, filter.PageSize, totalRecords);

        // Cache the response
        await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));

        return response;
    }



    public async Task<Response<GetEmployeeDto?>> GetEmployeeByIdAsync(int id)
    {
        var cacheKey = $"employee_{id}";

        // Try to get from cache first
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

        // Store in cache
        await _redisCache.SetAsync(cacheKey, employeeDto, TimeSpan.FromMinutes(15));

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
        
        // On update or deactivate
        await _redisCache.RemoveByPatternAsync("employees_*"); 
        await _redisCache.RemoveAsync($"employee_{employee.Id}");
        
        // 2. Department-related cache cleanup (NEW LOGIC)
        // Invalidates the specific department's nested employee list
        await _redisCache.RemoveAsync($"department_with_employees_{employee.DepartmentId}");
    
        // Invalidate the list/search views for all departments
        await _redisCache.RemoveByPatternAsync("departments_with_employees_*");


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
        
        // On update or deactivate
        await _redisCache.RemoveByPatternAsync("employees_*"); 
        await _redisCache.RemoveAsync($"employee_{employee.Id}");
        
        // 2. Department-related cache cleanup (NEW LOGIC)
        // Invalidates the specific department's nested employee list
        await _redisCache.RemoveAsync($"department_with_employees_{employee.DepartmentId}");
    
        // Invalidate the list/search views for all departments
        await _redisCache.RemoveByPatternAsync("departments_with_employees_*");


        return new Response<bool>(HttpStatusCode.OK, "Employee deactivated successfully", true);
    }
    
    
    private string GetEmployeesCacheKey(EmployeePaginationFilter filter)
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
