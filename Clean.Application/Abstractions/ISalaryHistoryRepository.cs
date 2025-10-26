using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryRepository
{
    Task<bool> AddAsync(SalaryHistory entity);
    Task<List<SalaryHistory>> GetSalaryHistoryByEmployeeIdAsync(int employeeId);
    // Task<List<SalaryHistory>> GetSalaryHistoryByEmailAsync(string email);
    Task<SalaryHistory?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
}