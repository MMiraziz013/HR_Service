using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationBalance;

namespace Clean.Application.Abstractions;

public interface IVacationBalanceService
{
    //TODO: Implement VacationBalance!

    Task AutoUpdateVacationBalancesAsync();
    
    Task<Response<GetVacationBalanceDto>> AddVacationBalanceAsync(AddVacationBalanceDto dto);

    Task<Response<List<GetVacationBalanceDto>>> GetAllVacationBalancesAsync(VacationBalanceFilter filter);
    Task<Response<List<GetVacationBalanceDto>>> GetLatestVacationBalancesAsync(VacationBalanceFilter filter);
    Task<Response<GetVacationBalanceDto>> GetVacationBalanceByEmployeeIdAsync(int employeeId);
    Task<Response<GetVacationBalanceDto>> GetVacationBalanceByIdAsync(int vacationBalanceId);

    Task<Response<GetVacationBalanceDto>> UpdateVacationBalanceAsync(UpdateVacationBalanceDto dto);
    
    //TODO: Check if delete is need for vacation balance.
}