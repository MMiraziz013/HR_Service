using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ISalaryAnomalyRepository
{
       Task<bool> AddAsync(SalaryAnomaly anomaly);
      Task<List<SalaryAnomaly>> GetAllAsync();
      Task<List<SalaryAnomaly>> GetByEmployeeIdAsync(int employeeId); 
      Task<SalaryAnomaly?> GetByIdAsync(int id);
      Task<List<SalaryAnomaly>> GetUnviewedAsync();
      Task<bool> MarkAsViewedAsync(int id);
      Task<bool> ExistsForEmployeeAndMonthAsync(int employeeId, DateOnly month);
      Task<bool> UpdateAsync(SalaryAnomaly salary);
      Task<bool> DeleteAnomalyAsync(int id);
}