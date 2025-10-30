using Clean.Application.Dtos.VacationBalance;

namespace Clean.Application.Abstractions;

public interface IVacationBalanceRepository
{
    //TODO: Implement IVacationBalanceRepository

    Task<GetVacationBalanceDto> AddAsync(AddVacationBalanceDto dto);
}