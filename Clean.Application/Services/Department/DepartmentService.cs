using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Department;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Responses;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.Department;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICacheService _redisCache;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(IDepartmentRepository departmentRepository, ICacheService redisCache, ILogger<DepartmentService> logger)
    {
        _departmentRepository = departmentRepository;
        _redisCache = redisCache;
        _logger = logger;
    }
    public async Task<Response<GetDepartmentDto>> AddDepartmentAsync(AddDepartmentDto dto)
    {
        try
        {
            var department = new Domain.Entities.Department
            {
                Name = dto.Name,
                Description = dto.Description
            };

            var isAdded = await _departmentRepository.AddDepartmentAsync(department);
            if (!isAdded)
            {
                return new Response<GetDepartmentDto>(HttpStatusCode.BadRequest, message: "Error while adding the department (maybe it already exists).");
            }

            await _redisCache.RemoveByPatternAsync("departments_*");

            var addedDepartment = new GetDepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description
            };

            return new Response<GetDepartmentDto>(HttpStatusCode.OK, message: "Department added successfully!", addedDepartment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(AddDepartmentAsync));
            return new Response<GetDepartmentDto>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    public async Task<Response<List<GetDepartmentDto>>> GetDepartmentsAsync(string? search = null)
    {
        try
        {
            var cacheKey = $"departments_all_{search ?? "null"}";

            var cached = await _redisCache.GetAsync<List<GetDepartmentDto>>(cacheKey);
            if (cached != null)
            {
                return new Response<List<GetDepartmentDto>>(HttpStatusCode.OK, "Departments retrieved successfully (from cache)!", cached);
            }

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

            await _redisCache.SetAsync(cacheKey, departmentDtos, TimeSpan.FromHours(1));

            return new Response<List<GetDepartmentDto>>(HttpStatusCode.OK, "Departments retrieved successfully!", departmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(GetDepartmentsAsync));
            return new Response<List<GetDepartmentDto>>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    public async Task<Response<List<GetDepartmentWithEmployeesDto>>> GetDepartmentsWithEmployeesAsync(string? search = null)
    {
        try
        {
            var cacheKey = $"departments_with_employees_{search ?? "null"}";

            var cached = await _redisCache.GetAsync<List<GetDepartmentWithEmployeesDto>>(cacheKey);
            if (cached != null)
            {
                return new Response<List<GetDepartmentWithEmployeesDto>>(HttpStatusCode.OK, "Departments with employees retrieved successfully (from cache)!", cached);
            }

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
                }).Where(e=> e.IsActive).ToList()
            }).ToList();

            await _redisCache.SetAsync(cacheKey, departmentDtos, TimeSpan.FromHours(1));

            return new Response<List<GetDepartmentWithEmployeesDto>>(HttpStatusCode.OK, "Departments with employees retrieved successfully!", departmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(GetDepartmentsWithEmployeesAsync));
            return new Response<List<GetDepartmentWithEmployeesDto>>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    public async Task<Response<List<GetDepartmentSummaryDto>>> GetDepartmentsSummaryAsync(string? search)
    {
        try
        {
            var cacheKey = $"departments_summary_{search ?? "null"}";

            var cached = await _redisCache.GetAsync<List<GetDepartmentSummaryDto>>(cacheKey);
            if (cached != null)
            {
                return new Response<List<GetDepartmentSummaryDto>>(HttpStatusCode.OK, "Departments summary retrieved successfully (from cache)!", cached);
            }

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

            await _redisCache.SetAsync(cacheKey, departmentDtos, TimeSpan.FromHours(1));

            return new Response<List<GetDepartmentSummaryDto>>(HttpStatusCode.OK, departmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(GetDepartmentsSummaryAsync));
            return new Response<List<GetDepartmentSummaryDto>>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    //TODO: Add Redis caching for this function
    public async Task<Response<List<GetDepartmentPaymentsDto>>> GetDepartmentsPaymentAsync()
    {
        try
        {
            var departments = await _departmentRepository.GetDepartmentsAsync();
            var departmentWithPayments = departments.Select(d => new GetDepartmentPaymentsDto
            {
                Id = d.Id,
                Name = d.Name,
                TotalAmount = d.Employees.Sum(e =>
                    e.SalaryHistories.OrderByDescending(s => s.Month).Select(s => s.ExpectedTotal).FirstOrDefault())
            }).ToList();


            return new Response<List<GetDepartmentPaymentsDto>>(HttpStatusCode.OK, departmentWithPayments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(GetDepartmentsSummaryAsync));
            return new Response<List<GetDepartmentPaymentsDto>>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
        
    }

    public async Task<Response<GetDepartmentDto?>> GetDepartmentByIdAsync(int id)
    {
        try
        {
            var cacheKey = $"department_{id}";

            var cachedDepartment = await _redisCache.GetAsync<GetDepartmentDto>(cacheKey);
            if (cachedDepartment != null)
            {
                return new Response<GetDepartmentDto?>(HttpStatusCode.OK, cachedDepartment);
            }

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

            await _redisCache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1));

            return new Response<GetDepartmentDto?>(HttpStatusCode.OK, "Department retrieved successfully.", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(GetDepartmentByIdAsync));
            return new Response<GetDepartmentDto?>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }
    
    public async Task<Response<GetDepartmentWithEmployeesDto?>> GetDepartmentByIdWithEmployeesAsync(int id)
    {
        try
        {
            var cacheKey = $"department_with_employees_{id}";

            var cachedDepartment = await _redisCache.GetAsync<GetDepartmentWithEmployeesDto>(cacheKey);
            if (cachedDepartment != null)
            {
                return new Response<GetDepartmentWithEmployeesDto?>(HttpStatusCode.OK, cachedDepartment);
            }

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

            await _redisCache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1));

            return new Response<GetDepartmentWithEmployeesDto?>(HttpStatusCode.OK, "Department retrieved successfully.", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(GetDepartmentByIdWithEmployeesAsync));
            return new Response<GetDepartmentWithEmployeesDto?>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    public async Task<Response<GetDepartmentDto>> UpdateDepartmentAsync(UpdateDepartmentDto dto)
    {
        try
        {
            var existing = await _departmentRepository.GetDepartmentByIdAsync(dto.Id);
            if (existing == null)
            {
                return new Response<GetDepartmentDto>(HttpStatusCode.NotFound, "Department not found.");
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                existing.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.Description))
            {
                existing.Description = dto.Description;
            }

            var isUpdated = await _departmentRepository.UpdateDepartmentAsync(existing);
            if (!isUpdated)
            {
                return new Response<GetDepartmentDto>(HttpStatusCode.InternalServerError, "Failed to update department.");
            }

            await _redisCache.RemoveByPatternAsync("departments_*");
            await _redisCache.RemoveByPatternAsync("departments_summary_*");
            await _redisCache.RemoveAsync($"department_with_employees_{existing.Id}");
            await _redisCache.RemoveAsync($"department_{existing.Id}");

            var updatedDto = new GetDepartmentDto
            {
                Id = existing.Id,
                Name = existing.Name,
                Description = existing.Description
            };

            return new Response<GetDepartmentDto>(HttpStatusCode.OK, "Department updated successfully!", updatedDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(UpdateDepartmentAsync));
            return new Response<GetDepartmentDto>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    public async Task<Response<bool>> DeleteDepartmentAsync(int id)
    {
        try
        {
            var isDeleted = await _departmentRepository.DeleteDepartmentAsync(id);
            if (!isDeleted)
            {
                return new Response<bool>(HttpStatusCode.BadRequest, "Failed to delete department or department not found.");
            }

            await _redisCache.RemoveByPatternAsync("departments_*");
            await _redisCache.RemoveAsync($"department_with_employees_{id}");
            await _redisCache.RemoveAsync($"department_{id}");

            return new Response<bool>(HttpStatusCode.OK, "Department deleted successfully!", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(DeleteDepartmentAsync));
            return new Response<bool>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }
}
