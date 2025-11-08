using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.SalaryHistory;

public class SalaryHistoryService : ISalaryHistoryService
{
    private readonly ISalaryHistoryRepository _repository;
private readonly IEmployeeRepository _employeeRepository;
private readonly IDepartmentRepository _departmentRepository;
private readonly ILogger<SalaryHistoryService> _logger;
private readonly ICacheService _redisCache;

public SalaryHistoryService(ISalaryHistoryRepository repository, 
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILogger<SalaryHistoryService> logger,
        ICacheService redisCache)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _logger = logger;
        _redisCache = redisCache;
    }
    /// <summary>
    /// Job method to generate salary history for the current month based on the previous month's data.
    /// </summary>
    public async Task GenerateMonthlySalaryHistoryAsync()
    {
        try
        {
            var employees = await _employeeRepository.GetActiveEmployeesAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            
            var existingSalaries = await _repository.GetSalaryHistoriesAsync(
                new SalaryHistoryFilter{
                    FromMonth = currentMonth,
                    ToMonth = currentMonth});

            foreach (var employee in employees)
            {
                if (existingSalaries.Any(s => s.EmployeeId == employee.Id))
                    continue;
                
                var lastSalary = await _repository.GetLatestSalaryHistoryAsync(employee.Id);

                if (lastSalary == null)
                {
                    _logger.LogWarning("Skipping EmployeeId {Id}: no previous salary record found.", employee.Id);
                    continue;
                }

                var dto = new AddSalaryHistoryDto
                {
                    EmployeeId = employee.Id,
                    Month = currentMonth,
                    BaseAmount = lastSalary.BaseAmount,
                };

                // Note: AddSalaryHistoryAsync has its own try-catch and logging.
                await AddSalaryHistoryAsync(dto);

                _logger.LogInformation("Auto-created salary history for EmployeeId {Id} for {Month}", 
                    employee.Id, currentMonth);
            }
            
            await _redisCache.RemoveByPatternAsync("salary_histories_"); 
            await _redisCache.RemoveByPatternAsync("latest_salary_histories_");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during job execution: GenerateMonthlySalaryHistoryAsync.");
        }
    }

    /// <summary>
    /// Adds a new salary history record.
    /// </summary>
    public async Task<Response<GetSalaryHistoryDto>> AddSalaryHistoryAsync(AddSalaryHistoryDto dto)
    {
        try
        {
            var thisMonth = DateTime.UtcNow.Month;
            var thisYear = DateTime.UtcNow.Year;
            var employee=await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
            if (employee == null)
            {
                return new Response<GetSalaryHistoryDto>(
                    HttpStatusCode.BadRequest,
                    message: "Employee with this id is not found");
            }
            
            var currentMonth=DateOnly.FromDateTime(DateTime.Now);
            var exists = await _repository.ExistForMonth(employee.Id,currentMonth);
            if (exists)
            {
                return new Response<GetSalaryHistoryDto>(HttpStatusCode.BadRequest,
                    $"Salary for employee with ID {employee.Id} is already added!");
            }
            
            var entity = new Domain.Entities.SalaryHistory
            {
                EmployeeId = dto.EmployeeId,
                BaseAmount = dto.BaseAmount,
                Month = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            
            if (entity.Month.Year < thisYear ||
                (entity.Month.Year == thisYear && entity.Month.Month < thisMonth))
            {
                return new Response<GetSalaryHistoryDto>(
                    HttpStatusCode.BadRequest,
                    "Cannot add salary for the previous months. Only current are allowed.");
            }
            var isAdded = await _repository.AddAsync(entity);

            if (!isAdded)
            {
                return new Response<GetSalaryHistoryDto>(
                    HttpStatusCode.InternalServerError,
                    message: "Failed to add salary history."
                    
                );
            }
            
            // Cache Invalidation
            await _redisCache.RemoveByPatternAsync("salary_histories_"); 
            await _redisCache.RemoveByPatternAsync($"salary_history_{entity.Id}"); 
            await _redisCache.RemoveByPatternAsync($"salary_histories_employee_{dto.EmployeeId}");
            await _redisCache.RemoveByPatternAsync("latest_salary_histories_");
            await _redisCache.RemoveByPatternAsync("total_paid_dept_"); 
            
            return new Response<GetSalaryHistoryDto>(
                HttpStatusCode.OK,
                message: "Salary history added successfully.",
                new GetSalaryHistoryDto
                {
                    Id=entity.Id,
                    BaseAmount = entity.BaseAmount,
                    BonusAmount = entity.BonusAmount,
                    ExpectedTotal = entity.ExpectedTotal,
                    Month = entity.Month,
                    EmployeeId = entity.EmployeeId
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding a new salary history record.");
            return new Response<GetSalaryHistoryDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while adding salary history.");
        }
    }

    /// <summary>
    /// Updates the base salary for the current month.
    /// </summary>
    public async Task<Response<UpdateSalaryDto>> UpdateSalaryHistoryAsync(UpdateSalaryDto dto)
    {
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
            if (employee == null)
                return new Response<UpdateSalaryDto>(HttpStatusCode.NotFound, "Employee is not found");

            var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);

            var salary = await _repository.GetSalaryByMonth(dto.EmployeeId, currentMonth);
            if (salary == null)
                return new Response<UpdateSalaryDto>(HttpStatusCode.NotFound, "Salary for current month is not found");

            salary.BaseAmount = dto.BaseSalary;

            var isUpdated = await _repository.UpdateSalaryAsync(salary);
            if (!isUpdated)
                return new Response<UpdateSalaryDto>(HttpStatusCode.InternalServerError, "Something went wrong, please try again.");

            var updatedDto = new UpdateSalaryDto
            {
                EmployeeId = salary.EmployeeId,
                BaseSalary = salary.BaseAmount
            };
            
            // Cache Invalidation
            await _redisCache.RemoveByPatternAsync("salary_histories_"); 
            await _redisCache.RemoveByPatternAsync($"salary_history_{salary.Id}"); 
            await _redisCache.RemoveByPatternAsync($"salary_histories_employee_{dto.EmployeeId}");
            await _redisCache.RemoveByPatternAsync("latest_salary_histories_");
            await _redisCache.RemoveByPatternAsync("total_paid_dept_");
            
            return new Response<UpdateSalaryDto>(HttpStatusCode.OK, "Base salary is updated successfully.", updatedDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating salary history.");
            return new Response<UpdateSalaryDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while updating salary history.");
        }
    }    
    
    /// <summary>
    /// Retrieves all salary history records for a specific employee.
    /// </summary>
    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByEmployeeIdAsync(int id)
    {
        try
        {
            var cacheKey = $"salary_histories_employee_{id}";
            var cached = await _redisCache.GetAsync<Response<List<GetSalaryHistoryWithEmployeeDto>>>(cacheKey);

            if (cached != null)
                return cached;

            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee is null)
            {
                return new Response<List<GetSalaryHistoryWithEmployeeDto>>(HttpStatusCode.NotFound,
                    $"Employee with ID {id} not found.");
            }

            var histories = await _repository.GetSalaryHistoryByEmployeeIdAsync(id);
            if (!histories.Any())
            {
                return new Response<List<GetSalaryHistoryWithEmployeeDto>>(HttpStatusCode.NotFound,
                    $"No salary history found for employee with ID {id}.");
            }

            var dtoList = histories.Select(h => new GetSalaryHistoryWithEmployeeDto
            {
                Id = h.Id,
                Month = h.Month,
                Base = h.BaseAmount,
                Bonus = h.BonusAmount,
                ExpectedTotal = h.ExpectedTotal,
                EmployeeName = h.Employee.FirstName,
                EmployeeId = h.EmployeeId
            }).ToList();

            var response = new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.OK,
                "Salary history retrieved successfully.",
                dtoList
            );

            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving salary history by employee ID {Id}.", id);
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving salary history.");
        }
    }

    /// <summary>
    /// Retrieves paginated salary history records based on a filter.
    /// </summary>
    public async Task<PaginatedResponse<GetSalaryHistoryDto>> GetAllAsync(SalaryHistoryFilter filter)
    {
        try
        {
            var cacheKey = $"salary_histories_all_{filter.GetHashCode()}";
            var cached = await _redisCache.GetAsync<PaginatedResponse<GetSalaryHistoryDto>>(cacheKey);

            if (cached != null)
            {
                return cached;
            }
            
            var salaries = await _repository.GetSalaryHistoriesAsync(filter);
            if (salaries.Count == 0)
            {
                return new PaginatedResponse<GetSalaryHistoryDto>(
                    new List<GetSalaryHistoryDto>(), // empty list
                    1,
                    1,
                    0
                )
                {
                    StatusCode = 404,
                    Message = "No salary records found."
                };
            }

            var mapped = salaries.Select(h => new GetSalaryHistoryDto
            {
                Id = h.Id,
                EmployeeId = h.EmployeeId,
                Month = h.Month,
                BaseAmount = h.BaseAmount,
                BonusAmount = h.BonusAmount,
                ExpectedTotal = h.ExpectedTotal,
                EmployeeName = $"{h.Employee.FirstName} {h.Employee.LastName}"

            }).ToList();
            
            var response = new PaginatedResponse<GetSalaryHistoryDto>(
                mapped,
                pageNumber: 1,
                pageSize: mapped.Count,
                totalRecords: mapped.Count
            )
            {
                StatusCode = 200,
                Message = "Salary records retrieved successfully."
            };
            
            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving all salary records.");
            return new PaginatedResponse<GetSalaryHistoryDto>(
                new List<GetSalaryHistoryDto>(), 1, 1, 0)
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while retrieving salary records."
            };
        }
    }    
    
    /// <summary>
    /// Retrieves a specific salary history record by employee ID and month (DateOnly).
    /// </summary>
    public async Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByMonthAsync(int employeeId, DateOnly month)
    {
        try
        {
            var cacheKey = $"salary_history_employee_{employeeId}_month_{month:yyyyMM}";
            var cached = await _redisCache.GetAsync<Response<GetSalaryHistoryWithEmployeeDto>>(cacheKey);

            if (cached != null)
            {
                return cached;
            }
            
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            if (employee is null)
            {
                return new Response<GetSalaryHistoryWithEmployeeDto>(
                    HttpStatusCode.NotFound,
                    message: $"Employee with ID {employeeId} not found."
                );
            }
            
            var salary = await _repository.GetSalaryByMonth(employeeId, month);
            if (salary is null)
            {
                return new Response<GetSalaryHistoryWithEmployeeDto>(
                    HttpStatusCode.NotFound,
                    message: $"No salary record found for {month:MMMM yyyy}."
                );
            }
            
            var dto = new GetSalaryHistoryWithEmployeeDto
            {
                Id = salary.Id,
                EmployeeId = salary.EmployeeId,
                Base = salary.BaseAmount,
                Bonus = salary.BonusAmount,
                ExpectedTotal = salary.ExpectedTotal,
                Month = salary.Month,
                EmployeeName = employee.FirstName
            };
            
            var response = new Response<GetSalaryHistoryWithEmployeeDto>(
                HttpStatusCode.OK,
                "Salary history retrieved successfully.",
                dto
            );
            
            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving salary history for Employee ID {Id} and Month {Month}.", employeeId, month);
            return new Response<GetSalaryHistoryWithEmployeeDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving salary history.");
        }
    }
    
    /// <summary>
    /// Calculates the total paid amount for a specific department and month.
    /// </summary>
    public async Task<Response<GetTotalPaidForDepartmentDto>> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month)
    {
        try
        {
            var cacheKey = $"total_paid_dept_{departmentId}_{month:yyyyMM}";

            var cached = await _redisCache.GetAsync<Response<GetTotalPaidForDepartmentDto>>(cacheKey);
            if (cached != null)
                return cached;

            var department = await _departmentRepository.GetDepartmentByIdAsync(departmentId);
            if (department is null)
                return new Response<GetTotalPaidForDepartmentDto>(HttpStatusCode.NotFound, "Department not found.");

            var total = await _repository.GetTotalPaidAmountByDepartmentAsync(departmentId, month);

            var dto = new GetTotalPaidForDepartmentDto
            {
                TotalPaidAmount = total,
                DepartmentId = departmentId,
                DepartmentName = department.Name,
                Month = month
            };

            var response = new Response<GetTotalPaidForDepartmentDto>(
                HttpStatusCode.OK,
                "Total paid amount for department retrieved successfully.",
                dto
            );

            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving total paid amount for Department ID {Id} and Month {Month}.", departmentId, month);
            return new Response<GetTotalPaidForDepartmentDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving total paid amount.");
        }
    }

    /// <summary>
    /// Retrieves all salary history records for a given month (DateTime).
    /// </summary>
    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByMonthAsync(DateTime month)
    {
        try
        {
            var cacheKey = $"salary_histories_month_{month:yyyyMM}";
            var cached = await _redisCache.GetAsync<Response<List<GetSalaryHistoryWithEmployeeDto>>>(cacheKey);

            if (cached != null)
            {
                return cached;
            }
            
            var history = await _repository.GetByMonthAsync(month);

            if (history.Count == 0)
            {
                return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                    HttpStatusCode.NotFound, // Changed from BadRequest to NotFound for better semantic meaning
                    message: $"No salary records were found for {month.Month}-{month.Year}");
            }

            var mapped = history.Select(h => new GetSalaryHistoryWithEmployeeDto
            {
                Id = h.Id,
                EmployeeId = h.EmployeeId,
                EmployeeName = h.Employee.FirstName,
                Base = h.BaseAmount,
                Bonus = h.BonusAmount,
                ExpectedTotal = h.ExpectedTotal,
                Month = h.Month
            }).ToList();
            
            var response = new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.OK,
                message: $"Salary history for {month.Month}-{month.Year} retrieved successfully", mapped);

            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving salary history for month {Month}.", month);
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving salary history.");
        }
    }
    
    /// <summary>
    /// Retrieves the latest salary history record for active employees.
    /// </summary>
    public async Task<Response<List<GetSalaryHistoryDto>>> GetLatestSalaryHistoriesAsync(SalaryHistoryFilter? filter = null)
    {
        try
        {
            var cacheKey = $"latest_salary_histories_filter_{filter?.EmployeeId ?? 0}";
            var cached = await _redisCache.GetAsync<Response<List<GetSalaryHistoryDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            var activeEmployees = await _employeeRepository.GetActiveEmployeesAsync();
            
            if (filter?.EmployeeId != null)
            {
                activeEmployees = activeEmployees
                    .Where(e => e.Id == filter.EmployeeId.Value)
                    .ToList();
            }

            var latestHistories = activeEmployees
                .Where(e => e.SalaryHistories.Any())
                .Select(e =>
                {
                    var latest = e.SalaryHistories
                        .OrderByDescending(s => s.Month)
                        .First();

                    return new GetSalaryHistoryDto
                    {
                        Id = latest.Id,
                        Month = latest.Month,
                        BaseAmount = latest.BaseAmount,
                        BonusAmount = latest.BonusAmount,
                        ExpectedTotal = latest.BaseAmount + latest.BonusAmount,
                        EmployeeId = latest.EmployeeId,
                        EmployeeName = latest.Employee.FirstName+" "+latest.Employee.LastName
                    };
                })
                .ToList();
            
            var response = new Response<List<GetSalaryHistoryDto>>(HttpStatusCode.OK, latestHistories);

            await _redisCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving latest salary histories.");
            return new Response<List<GetSalaryHistoryDto>>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving latest salary histories.");
        }
    }

    /// <summary>
    /// Retrieves a salary history record by its unique ID.
    /// </summary>
    public async Task<Response<GetSalaryHistoryDto>> GetByIdAsync(int id)
    {
        try
        {
            var cacheKey = $"salary_history_{id}";

            var cached = await _redisCache.GetAsync<GetSalaryHistoryDto>(cacheKey);
            if (cached != null)
            {
                return new Response<GetSalaryHistoryDto>(
                    HttpStatusCode.OK,
                    "Salary history retrieved successfully (from cache).",
                    cached);
            }

            var salary = await _repository.GetByIdAsync(id);
            if (salary is null)
            {
                return new Response<GetSalaryHistoryDto>(
                    HttpStatusCode.NotFound,
                    $"Salary history with ID {id} not found.");
            }

            var mapped = new GetSalaryHistoryDto
            {
                Id = salary.Id,
                BaseAmount = salary.BaseAmount,
                BonusAmount = salary.BonusAmount,
                ExpectedTotal = salary.ExpectedTotal,
                Month = salary.Month,
                EmployeeName = salary.Employee.FirstName + " " + salary.Employee.LastName
            };

            await _redisCache.SetAsync(cacheKey, mapped, TimeSpan.FromMinutes(15));

            return new Response<GetSalaryHistoryDto>(
                HttpStatusCode.OK,
                "Salary history retrieved successfully.",
                mapped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving salary history by ID {Id}.", id);
            return new Response<GetSalaryHistoryDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving salary history.");
        }
    }
    
    
    /// <summary>
    /// Applies a bonus percentage to all active employees in a department for the current month.
    /// </summary>
    public async Task<Response<List<DepartmentBonusAppliedDto>>> ApplyDepartmentBonusAsync(int departmentId, decimal bonusPercentage)
    {
        try
        {
            var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
            
            var employees = await _employeeRepository.GetActiveEmployeesByDepartmentAsync(departmentId);

            if (!employees.Any())
                return new Response<List<DepartmentBonusAppliedDto>>(HttpStatusCode.NotFound, "No active employees found in this department.", new List<DepartmentBonusAppliedDto>());

            var appliedBonuses = new List<DepartmentBonusAppliedDto>();

            foreach (var emp in employees)
            {
                var salary = await _repository.GetSalaryByMonth(emp.Id, currentMonth);

                if (salary != null)
                {
                    salary.BonusAmount = salary.BaseAmount * bonusPercentage / 100m;
                    
                    await _repository.UpdateSalaryAsync(salary);

                    appliedBonuses.Add(new DepartmentBonusAppliedDto
                    {
                        EmployeeId = emp.Id,
                        EmployeeName = $"{emp.FirstName} {emp.LastName}",
                        BaseAmount = salary.BaseAmount,
                        BonusAmount = salary.BonusAmount,
                        ExpectedTotal = salary.ExpectedTotal
                    });
                }
            }
            
            // Cache Invalidation
            await _redisCache.RemoveByPatternAsync("salary_histories_");
            await _redisCache.RemoveByPatternAsync("latest_salary_histories_");
            await _redisCache.RemoveByPatternAsync("total_paid_dept_");

            return new Response<List<DepartmentBonusAppliedDto>>(
                HttpStatusCode.OK,
                "Department bonuses applied successfully.",
                appliedBonuses
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while applying department bonus for Department ID {Id}.", departmentId);
            return new Response<List<DepartmentBonusAppliedDto>>(HttpStatusCode.InternalServerError, "An unexpected error occurred while applying department bonus.");
        }
    }

    
    //    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByIdAsync(int id)
//    {
//        var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
//     if (id is 0 || employee is null)
//     {
//         return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
//             HttpStatusCode.NotFound,
//             message: $"Employee with id {id} is not found.");
//     }
//
//     
//     var employeeHistories = await _repository.GetSalaryHistoryByEmployeeIdAsync(id);
//
//     if ( !employeeHistories.Any())
//     {
//         return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
//             HttpStatusCode.NotFound,
//             message: $"No salary history found for employee ID {id}."
//         );
//     }
//
//     var employeeDtos = employeeHistories.Select(h => new GetSalaryHistoryWithEmployeeDto
//     {
//         Id = h.Id,
//         EmployeeId = h.EmployeeId,
//         Month = h.Month,
//         ExpectedTotal = h.ExpectedTotal,
//         EmployeeName = $"{h.Employee.FirstName} {h.Employee.LastName}"
//     }).ToList();
//
//     return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
//         HttpStatusCode.OK,
//         message: "Salary history retrieved successfully.",
//         data: employeeDtos
//     );
// }
    
    // public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoriesAsync()
    // {
    //     var histories = await _repository.GetSalaryHistoriesAsync();
    //
    //     if (!histories.Any())
    //     {
    //         return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
    //             HttpStatusCode.NotFound,
    //             message: $"No salary history was found"
    //         );
    //     }
    //
    // var mapped = histories.Select(h => new GetSalaryHistoryWithEmployeeDto()
    // {
    //     Id = h.Id,
    //     Month = h.Month,
    //     ExpectedTotal = h.ExpectedTotal,
    //     EmployeeName = h.Employee.FirstName,
    //     EmployeeId = h.EmployeeId
    // }).ToList();
    //
    //     return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
    //         HttpStatusCode.OK,
    //         message: "Salary history retrieved successfully.",
    //         data: mapped
    //     );
    // }

    
    // public async Task<Response<bool>> DeleteSalaryHistoryAsync(int id)
    // {
    //     var isDeleted = await _repository.DeleteAsync(id);
    //
    //     if (!isDeleted)
    //     {
    //         return new Response<bool>(
    //             HttpStatusCode.NotFound,
    //             message: "Salary history not found or could not be deleted.",
    //             data: false
    //         );
    //     }
    //     
    //     return new Response<bool>(
    //         HttpStatusCode.OK,
    //         message: "Salary history deleted successfully.",
    //         data: true
    //     );
    // }

    
    // public async Task<Response<TotalPaidDto>> GetTotalPaidAmountAsync(int employeeId, DateTime startDate, DateTime endDate)
    // {
    //     //TODO: when employee service will be ready, if statement should be added which checks whether employee id exists in db 
    //     try
    //     {
    //         var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
    //         var total = await _repository.GetTotalPaidAmountAsync(employeeId, startDate, endDate);
    //
    //         var dto = new TotalPaidDto
    //         {
    //             TotalPaidAmount = total,
    //             EmployeeId = employeeId,
    //             StartDate = startDate,
    //             EndDate = endDate,
    //             EmployeeName = employee!.FirstName
    //         };
    //         return new Response<TotalPaidDto>(
    //             HttpStatusCode.OK,
    //             "Total paid amount retrieved successfully.",
    //             dto
    //         );
    //     }
    //     catch (Exception ex)
    //     {
    //         return new Response<TotalPaidDto>(
    //             HttpStatusCode.InternalServerError,
    //             message: $"An error occured: {ex.Message}" 
    //         );
    //     }
    // }

}