using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryService
{
    Task<Response<bool>> AddSalaryHistoryAsync(AddSalaryHistoryDto salary);
    Task<Response<List<GetSalaryHistoryDto>>> GetSalaryHistoryByEmployeeIdAsync(int id);
    Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByIdAsync(int id);
    Task<Response<bool>> DeleteSalaryHistoryAsync(int id);
    Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByMonthAsync(int employeeId, DateOnly month);
    Task<Response<TotalPaidDto>> GetTotalPaidAmountAsync(int employeeId, DateOnly startDate, DateOnly endDate);
    Task<Response<GetTotalPaidForDepartmentDto>> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly startDate, DateOnly endDate);
   
}