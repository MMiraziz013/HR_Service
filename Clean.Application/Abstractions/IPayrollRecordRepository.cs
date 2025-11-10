using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports.Payroll;
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
    Task<IEnumerable<PayrollRecord>> GetPayrollRecordsAsync(PayrollRecordFilter filter);
    Task<List<PayrollRecord?>> GetLatestPayrollAsync();
    Task<decimal> GetTotalPaidForMonth(DateOnly month);
    Task<decimal> GetPositionExpectedAverageAsync(int departmentId, string position);
    Task<decimal> GetDepartmentExpectedAverageAsync(int departmentId);

    Task<PayrollRecord?> GetPayrollByMonthAsync(int employeeId, DateOnly month);
    Task<List<PayrollRecord>> GetPayrollRecordsByMonthAsync(int year, int month);
    Task<List<PayrollRecord>> GetPayrollRecordsAsync(DateTime startMonth, DateTime endMonth);
    
    
    Task<IEnumerable<PayrollReportDto>> GetForReportAsync(int? employeeId, DateOnly? startDate,
        DateOnly? endDate, int? departmentId);
}