using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryService
{
    
    Task<Response<GetSalaryHistoryDto>> AddSalaryHistoryAsync(AddSalaryHistoryDto dto);
    Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByEmployeeIdAsync(int id);
    Task<PaginatedResponse<GetSalaryHistoryDto>> GetAllAsync(SalaryHistoryFilter filter);
    Task<Response<GetSalaryHistoryWithEmployeeDto>> GetSalaryHistoryByMonthAsync(int employeeId, DateOnly month);
    Task<Response<GetTotalPaidForDepartmentDto>> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month);
    Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoryByMonthAsync(DateTime month);
    Task<Response<List<GetSalaryHistoryDto>>> GetLatestSalaryHistoriesAsync(SalaryHistoryFilter? filter = null);
    Task GenerateMonthlySalaryHistoryAsync();

    Task<Response<GetSalaryHistoryDto>> GetByIdAsync(int id);
    //Task<Response<List<GetSalaryHistoryWithEmployeeDto>>> GetSalaryHistoriesAsync();
    // Task<Response<bool>> DeleteSalaryHistoryAsync(int id);
    // Task<Response<TotalPaidDto>> GetTotalPaidAmountAsync(int employeeId, DateTime startDate, DateTime endDate);
    // Task<Response<GetTotalPaidForDepartmentDto>> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month);
}