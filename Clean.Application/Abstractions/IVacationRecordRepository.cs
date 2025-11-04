using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationRecords;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVacationRecordRepository
{
    Task<VacationRecord?> AddAsync(VacationRecord vacationRecord);

    Task<(List<GetVacationRecordDto> vacationRecord, int totalRecords)> GetAllAsync(
        VacationRecordPaginationFilter filter);
    
    Task<List<VacationRecord>> GetAllBetweenDatesAsync(DateOnly startDate, DateOnly endDate);

    Task<VacationRecord?> GetByIdAsync(int id);
    Task<VacationRecord?> UpdateAsync(VacationRecord recordToUpdate);
    Task<bool> Delete(int id);

    Task<List<VacationRecord>> GetVacationsToFinishAsync(DateOnly today);
}