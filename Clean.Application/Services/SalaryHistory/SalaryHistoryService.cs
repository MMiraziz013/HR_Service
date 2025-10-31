using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;

namespace Clean.Application.Services.SalaryHistory;

public class SalaryHistoryService : ISalaryHistoryService
{
    private readonly ISalaryHistoryRepository _repository;
private readonly IEmployeeRepository _employeeRepository;
private readonly IDepartmentRepository _departmentRepository;
    public SalaryHistoryService(ISalaryHistoryRepository repository, IEmployeeRepository employeeRepository,IDepartmentRepository departmentRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
    }
    
    public async Task<Response<bool>> AddSalaryHistoryAsync(AddSalaryHistoryDto dto)
    {
        
            var employee=await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
            if (employee == null)
            {
                return new Response<bool>(
                    HttpStatusCode.BadRequest,
                    message: "Employee with this id is not found");
            } 
            var entity = new Domain.Entities.SalaryHistory
            {
                EmployeeId = dto.EmployeeId,
                BaseAmount = dto.BaseAmount,
                BonusAmount = dto.BonusAmount,
                Month = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            
            var isAdded = await _repository.AddAsync(entity);

            if (!isAdded)
            {
                return new Response<bool>(
                    HttpStatusCode.InternalServerError,
                    message: "Failed to add salary history.",
                    data: false
                );
            }

            return new Response<bool>(
                HttpStatusCode.OK,
                message: "Salary history added successfully.",
                data: true
            );
    }


  
    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByEmployeeIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.BadRequest,
                message: "Invalid Employee ID. Please provide a valid ID."
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
    
    public async Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByIdAsync(int? id)
    {
        if (id is null or 0)
        {
            var allHistories = await _repository.GetSalaryHistoriesAsync();

            if (!allHistories.Any())
            {
                return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                    HttpStatusCode.NotFound,
                    message: "No salary history records found in the database."
                );
            }

            var dtos = allHistories.Select(h => new GetSalaryHistoryWithEmployeeDto
            {
                Id = h.Id,
                Month = h.Month,
                EmployeeId = h.EmployeeId,
                EmployeeName = h.Employee.FirstName,
                ExpectedTotal = h.ExpectedTotal
            }).ToList();

            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.OK,
                message: "All salary histories retrieved successfully.",
                data: dtos
            );
        }

        var history = await _repository.GetByIdAsync(id.Value);

        if (history == null)
        {
            return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
                HttpStatusCode.NotFound,
                message: $"Salary history with ID {id} not found."
            );
        }

        var dto = new GetSalaryHistoryWithEmployeeDto
        {
            Id = history.Id,
            EmployeeId = history.EmployeeId,
            Month = history.Month,
            ExpectedTotal = history.ExpectedTotal,
            EmployeeName = history.Employee.FirstName
        };
        
        return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
            HttpStatusCode.OK,
            message: "Salary history retrieved successfully.",
            data: [dto]
        );
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
            ExpectedTotal = h.ExpectedTotal,
            EmployeeId = h.EmployeeId,
            EmployeeName = h.Employee.FirstName,
            Month = h.Month
        }).ToList();

        return new Response<List<GetSalaryHistoryWithEmployeeDto>>(
            HttpStatusCode.OK,
            message: $"Salary history for {month.Month}-{month.Year} retrieved successfully", mapped);

    }
    
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