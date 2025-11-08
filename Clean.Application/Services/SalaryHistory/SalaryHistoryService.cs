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
    public SalaryHistoryService(ISalaryHistoryRepository repository, IEmployeeRepository employeeRepository,IDepartmentRepository departmentRepository,ILogger<SalaryHistoryService> logger)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _logger = logger;
    }
    public async Task GenerateMonthlySalaryHistoryAsync()
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

            await AddSalaryHistoryAsync(dto);

            _logger.LogInformation("Auto-created salary history for EmployeeId {Id} for {Month}", 
                employee.Id, currentMonth);
        }
    }

    public async Task<Response<GetSalaryHistoryDto>> AddSalaryHistoryAsync(AddSalaryHistoryDto dto)
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

    public async Task<Response<UpdateSalaryDto>> UpdateSalaryHistoryAsync(UpdateSalaryDto dto)
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

        return new Response<UpdateSalaryDto>(HttpStatusCode.OK, "Base salary is updated successfully.", updatedDto);

    }
    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByEmployeeIdAsync(int id)
    {
        if (id is 0)
        {
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.BadRequest,
                message: $"Employee id cannot be 0."
            );
        }

        var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
        if (employee is null)
        {
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.NotFound,
                message: $"Employee with ID {id} not found."
            );
        }

        var histories = await _repository.GetSalaryHistoryByEmployeeIdAsync(id);
        if (!histories.Any())
        {
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.NotFound,
                message: $"No salary history found for employee with ID {id}."
            );
        }

        var dtoList = histories.Select(h => new GetSalaryHistoryWithEmployeeDto()
        {
            Id = h.Id,
            Month = h.Month,
            Base = h.BaseAmount,
            Bonus = h.BonusAmount,
            ExpectedTotal = h.ExpectedTotal,
            EmployeeName = h.Employee.FirstName,
            EmployeeId = h.EmployeeId
        }).ToList();

        return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
            HttpStatusCode.OK,
            message: "Salary history retrieved successfully.",
            data: dtoList
        );
    }

    public async Task<PaginatedResponse<GetSalaryHistoryDto>> GetAllAsync(SalaryHistoryFilter filter)
    {
        var salaries = await _repository.GetSalaryHistoriesAsync(filter);
        if ( !salaries.Any())
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

        // TODO: Base Amount and Bonus Amount are not mapped
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
        
        return new PaginatedResponse<GetSalaryHistoryDto>(
            mapped,
            pageNumber: 1,
            pageSize: mapped.Count,
            totalRecords: mapped.Count
        )
        {
            StatusCode = 200,
            Message = "Salary records retrieved successfully."
        };

    }
    
    public async Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByMonthAsync(int employeeId, DateOnly month)
    {
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

        return new Response<GetSalaryHistoryWithEmployeeDto>(
            HttpStatusCode.OK,
            "Salary history retrieved successfully.",
            dto
        );
    }
    
    public async Task<Response<GetTotalPaidForDepartmentDto>> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month)
    {
           var department = await _departmentRepository.GetDepartmentByIdAsync(departmentId);
           if (department is null)
           {
               return new Response<GetTotalPaidForDepartmentDto>(
                   HttpStatusCode.NotFound,
                   "Department is not found.");
           }
           
           //TODO: Implement edge case handling when the month input is null, or not chosen
           var total = await _repository.GetTotalPaidAmountByDepartmentAsync(departmentId, month);
        
           var dto = new GetTotalPaidForDepartmentDto
           {
               TotalPaidAmount = total,
               DepartmentId = departmentId,
               Month = month,
               DepartmentName = department.Name
           };
           return new Response<GetTotalPaidForDepartmentDto>(
               HttpStatusCode.OK,
               message: "Total paid amount for department retrieved successfully.",
               dto
           );
           
    }

    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByMonthAsync(DateTime month)
    {
        var history = await _repository.GetByMonthAsync(month);

        if (history.Count == 0)
        {
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.BadRequest,
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

        return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
            HttpStatusCode.OK,
            message: $"Salary history for {month.Month}-{month.Year} retrieved successfully", mapped);

    }

    public async Task<Response<List<GetSalaryHistoryDto>>> GetLatestSalaryHistoriesAsync(SalaryHistoryFilter? filter = null)
    {

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

        return new Response<List<GetSalaryHistoryDto>>(HttpStatusCode.OK, latestHistories);
    }

    public async Task<Response<GetSalaryHistoryDto>> GetByIdAsync(int id)
    {
        var salary = await _repository.GetByIdAsync(id);
        if (salary is null)
        {
            return new Response<GetSalaryHistoryDto>(
                HttpStatusCode.NotFound,
                $"Salary history with ID {id} is not found.");
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

        return new Response<GetSalaryHistoryDto>(
            HttpStatusCode.OK,
            "Salary history retrieved successfully.",
            mapped);
    }
    
    
    public async Task<Response<List<DepartmentBonusAppliedDto>>> ApplyDepartmentBonusAsync(int departmentId, decimal bonusPercentage)
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

        return new Response<List<DepartmentBonusAppliedDto>>(
            HttpStatusCode.OK,
            "Department bonuses applied successfully.",
            appliedBonuses
        );
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