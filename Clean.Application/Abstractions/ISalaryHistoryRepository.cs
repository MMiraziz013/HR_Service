using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports.SalaryHistory;
using Clean.Application.Dtos.SalaryHistory;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryRepository
{
    Task<bool> AddAsync(SalaryHistory entity);
    Task<List<SalaryHistory>> GetSalaryHistoryByEmployeeIdAsync(int employeeId);
    Task<List<SalaryHistory>> GetSalaryHistoriesAsync(SalaryHistoryFilter filter);
    Task<SalaryHistory?> GetByIdAsync(int id);
    Task<List<SalaryHistory>> GetByMonthAsync(DateTime month);
    Task<SalaryHistory?> GetSalaryByMonth(int employeeId, DateOnly month);
      Task<decimal> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month);
      Task<SalaryHistory?> GetLatestSalaryHistoryAsync(int employeeId);
      Task<bool> ExistForMonth(int employeeId, DateOnly month);
      Task<decimal> GetDepartmentAverageSalaryAsync(int departmentId);
      Task<List<SalaryHistory?>> GetLatestSalaryHistoriesAsync();
      Task<bool> UpdateSalaryAsync(SalaryHistory salary);

      Task<IEnumerable<SalaryHistoryDto>> GetForReportAsync(int? employeeId, int? departmentId, DateOnly? fromMonth,
          DateOnly? toMonth);
    

}