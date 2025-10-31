using Clean.Application.Dtos.SalaryHistory;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryRepository
{
    Task<bool> AddAsync(SalaryHistory entity);
    Task<List<SalaryHistory>> GetSalaryHistoryByEmployeeIdAsync(int employeeId);
    Task<List<SalaryHistory>> GetSalaryHistoriesAsync();
    Task<SalaryHistory?> GetByIdAsync(int id);
    Task<List<SalaryHistory>> GetByMonthAsync(DateTime month);
    Task<SalaryHistory?> GetSalaryByMonth(int employeeId, DateOnly month);
      Task<decimal> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month);
      Task<SalaryHistory?> GetLatestSalaryHistoryAsync(int employeeId);
      
      
      // Task<bool> ExistForMonth(int employeeId, DateTime month);
      // Task<decimal> GetTotalPaidAmountAsync(int employeeId, DateTime startDate, DateTime endDate);
      // Task<bool> DeleteAsync(int id);
      // Task<List<SalaryHistory>> GetSalaryHistoryByEmailAsync(string email);


}