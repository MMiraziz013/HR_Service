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
    public SalaryHistoryService(ISalaryHistoryRepository repository, IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }
    
    public async Task<Response<bool>> AddSalaryHistoryAsync(AddSalaryHistoryDto dto)
    {
            
        try
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
                Month = dto.Month
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
        catch (ArgumentException ex)
        {
           // Log.Warning(ex, "Validation error while adding salary history for employee {EmployeeId}", dto.EmployeeId);
            return new Response<bool>(
                HttpStatusCode.BadRequest,
                new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
           // Log.Error(ex, "Unexpected error while adding salary history for employee {EmployeeId}", dto.EmployeeId);
            return new Response<bool>(
                HttpStatusCode.InternalServerError,
                new List<string> { "An unexpected error occurred." }
            );
        }
    }

    
    public async Task<Response<List<GetSalaryHistoryDto>>> GetSalaryHistoryByEmployeeIdAsync(int id)
    {
        try
        {
            var histories = await _repository.GetSalaryHistoryByEmployeeIdAsync(id);

            if (!histories.Any())
            {
                return new Response<List<GetSalaryHistoryDto>>(
                    HttpStatusCode.NotFound,
                    message: $"No salary history found for employee with ID {id}."
                );
            }

            var dtoList = histories.Select(h => new GetSalaryHistoryDto
            {
                Id=h.Id,
                Month = h.Month,
                BaseAmount = h.BaseAmount,
                BonusAmount = h.BonusAmount,
                ExpectedTotal = h.ExpectedTotal,
            }).ToList();

            return new Response<List<GetSalaryHistoryDto>>(
                HttpStatusCode.OK,
                message: "Salary history retrieved successfully.",
                data: dtoList
            );
        }
        catch (Exception ex)
        {
           // Log.Error(ex, "Error retrieving salary history for employee {EmployeeId}", id);
            return new Response<List<GetSalaryHistoryDto>>(
                HttpStatusCode.InternalServerError,
                new List<string> { "An unexpected error occurred while retrieving salary history." }
            );
        }
    }


    public async Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByIdAsync(int id)
    {
        try
        {
            var history = await _repository.GetByIdAsync(id);

            if (history == null)
            {
                return new Response<GetSalaryHistoryWithEmployeeDto>(
                    HttpStatusCode.NotFound,
                    new List<string> { $"Salary history with ID {id} not found." }
                );
            }
             var employee=await _employeeRepository.GetEmployeeByIdAsync(history.EmployeeId);
          

             var dto = new GetSalaryHistoryWithEmployeeDto
            {
                Id = history.Id,
                Month = history.Month,
                ExpectedTotal = history.ExpectedTotal,
                EmployeeName=employee!.FirstName
            };

            return new Response<GetSalaryHistoryWithEmployeeDto>(
                HttpStatusCode.OK,
                "Salary history retrieved successfully.",
                dto
            );
        }
        catch (Exception)
        {
            return new Response<GetSalaryHistoryWithEmployeeDto>(
                HttpStatusCode.InternalServerError,
                new List<string> { "An unexpected error occurred while retrieving salary history." }
            );
        }
    }


    public async Task<Response<bool>> DeleteSalaryHistoryAsync(int id)
    {
        var isDeleted = await _repository.DeleteAsync(id);

        if (!isDeleted)
        {
            return new Response<bool>(
                HttpStatusCode.NotFound,
                message: "Salary history not found or could not be deleted.",
                data: false
            );
        }
        
        return new Response<bool>(
            HttpStatusCode.OK,
            message: "Salary history deleted successfully.",
            data: true
        );
    }

    public async Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByMonthAsync(int employeeId, DateOnly month)
    {
        //TODO: when employee service will be ready, if statement should be added which checks whether employee id exists in db 
        
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            var salary = await _repository.GetSalaryByMonth(employeeId, month);

            // Map entity to DTO
            var dto = new GetSalaryHistoryWithEmployeeDto
            {
                Id = salary.Id,
                EmployeeId = salary.EmployeeId,
                ExpectedTotal = salary.ExpectedTotal,
                Month = salary.Month,
                EmployeeName = employee!.FirstName
            };

            return new Response<GetSalaryHistoryWithEmployeeDto>(
                HttpStatusCode.OK,
                "Salary history retrieved successfully.",
                dto
            );
        }
        catch (ArgumentException ex)
        {
            return new Response<GetSalaryHistoryWithEmployeeDto>(
                HttpStatusCode.NotFound,
                ex.Message
            );
        }
    }

  
    public async Task<Response<TotalPaidDto>> GetTotalPaidAmountAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        //TODO: when employee service will be ready, if statement should be added which checks whether employee id exists in db 
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            var total = await _repository.GetTotalPaidAmountAsync(employeeId, startDate, endDate);

            var dto = new TotalPaidDto
            {
                TotalPaidAmount = total,
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate,
                EmployeeName = employee!.FirstName
            };
            return new Response<TotalPaidDto>(
                HttpStatusCode.OK,
                "Total paid amount retrieved successfully.",
                dto
            );
        }
        catch (Exception ex)
        {
            return new Response<TotalPaidDto>(
                HttpStatusCode.InternalServerError,
                message: $"An error occured: {ex.Message}" 
            );
        }
    }

    public async Task<Response<GetTotalPaidForDepartmentDto>> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly startDate, DateOnly endDate)
    {
       // TODO : add department name when department service is ready, so not only dep id is shown but also name 
       try
       {
           var department = await _departmentRepository.GetDepartmentByIdAsync(departmentId);
           var total = await _repository.GetTotalPaidAmountByDepartmentAsync(departmentId, startDate, endDate);

           var dto = new GetTotalPaidForDepartmentDto
           {
               TotalPaidAmount = total,
               DepartmentId = departmentId,
               StartDate = startDate,
               EndDate = endDate,
               DepartmentName = department!.Name
           };
           return new Response<GetTotalPaidForDepartmentDto>(
               HttpStatusCode.OK,
               message: "Total paid amount for department retrieved successfully.",
               dto
           );

       }
       catch (Exception ex)
       {
           return new Response<GetTotalPaidForDepartmentDto>(
               HttpStatusCode.InternalServerError,
               message: $"An error occured: {ex.Message}" 
           );
       }
       
    }
}