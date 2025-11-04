using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationBalance;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVacationBalanceRepository
{
    Task<bool> AddVacationBalanceAsync(VacationBalance dto);
    Task<List<VacationBalance>> GetVacationBalancesAsync(VacationBalanceFilter filter);

    Task<VacationBalance?> GetVacationBalanceByIdAsync(int vacationBalanceId);
    
    // Task<VacationBalance> GetVacationBalanceByEmployeeIdAsync(int employeeId);
    Task<VacationBalance?> UpdateVacationBalanceAsync(VacationBalance dto);
    
    Task<bool> ExistsAsync(int employeeId, int year);
}