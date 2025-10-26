using Clean.Application.Dtos.SalaryHistory;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryRepository
{
    Task<bool> AddAsync(SalaryHistory entity);
    Task<List<SalaryHistory>> GetSalaryHistoryByEmployeeIdAsync(int employeeId);
    // Task<List<SalaryHistory>> GetSalaryHistoryByEmailAsync(string email);
    Task<SalaryHistory?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<SalaryHistory> GetSalaryByMonth(int employeeId, DateOnly month);
    Task<bool> ExistForMonth(int employeeId, DateOnly month);
    Task<decimal> GetTotalPaidAmountAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateTime startDate, DateTime endDate);

    

}