using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IPayrollRecordRepository
{
    Task<bool> AddAsync(PayrollRecord payroll);
    Task<PayrollRecord?> GetByIdAsync(int id);
    Task<List<PayrollRecord>> GetAllAsync();
    Task<List<PayrollRecord>> GetByEmployeeIdAsync(int employeeId);
    Task<PayrollRecord?> GetLatestByEmployeeIdAsync(int employeeId);
    Task<bool> UpdateAsync(PayrollRecord payroll);
    Task<bool> DeleteAsync(int id);
}