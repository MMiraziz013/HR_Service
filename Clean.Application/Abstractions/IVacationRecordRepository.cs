using Clean.Application.Dtos.VacationRecords;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVacationRecordRepository
{
    public Task<bool> AddAsync(AddVacationRecordDto dto);

    public Task<List<VacationRecord>> GetVacationRecordsAsync();
}