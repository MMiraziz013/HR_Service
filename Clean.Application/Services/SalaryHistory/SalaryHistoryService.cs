using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;

namespace Clean.Application.Services.SalaryHistory;

public class SalaryHistoryService : ISalaryHistoryService
{
    private readonly ISalaryHistoryRepository _repository;

    public SalaryHistoryService(ISalaryHistoryRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Response<bool>> AddSalaryHistoryAsync(AddSalaryHistoryDto dto)
    {
        try
        {
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

    
    public async Task<Response<List<GetSalaryHistoryDto>>> GetSalaryHistoryByEmployeeId(int id)
    {
        try
        {
            var histories = await _repository.GetSalaryHistoryByEmployeeIdAsync(id);

            if (histories == null || !histories.Any())
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
                ExpectedTotal = h.ExpectedTotal
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


    public async Task<Response<GetSalaryHistoryDto>> GetSalaryHistoryById(int id)
    {
        try
        {
            var history = await _repository.GetByIdAsync(id);

            if (history == null)
            {
                return new Response<GetSalaryHistoryDto>(
                    HttpStatusCode.NotFound,
                    new List<string> { $"Salary history with ID {id} not found." }
                );
            }

            var dto = new GetSalaryHistoryDto
            {
                Id = history.Id,
                Month = history.Month,
                BaseAmount = history.BaseAmount,
                BonusAmount = history.BonusAmount,
                ExpectedTotal = history.ExpectedTotal,
                // Optional: include employee info if your DTO supports it
                //EmployeeName = history.Employee.FullName
            };

            return new Response<GetSalaryHistoryDto>(
                HttpStatusCode.OK,
                "Salary history retrieved successfully.",
                dto
            );
        }
        catch (Exception)
        {
            return new Response<GetSalaryHistoryDto>(
                HttpStatusCode.InternalServerError,
                new List<string> { "An unexpected error occurred while retrieving salary history." }
            );
        }
    }


    public Task<Response<bool>> DeleteSalaryHistory(int id)
    {
        throw new NotImplementedException();
    }
}