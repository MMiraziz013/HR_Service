using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryHistory;

namespace Clean.Application.Abstractions;

public interface ISalaryHistoryService
{
    public Task<Response<bool>> AddSalaryHistoryAsync(AddSalaryHistoryDto salary);
    public Task<Response<List<GetSalaryHistoryDto>>> GetSalaryHistoryByEmployeeId(int id);
    public Task<Response<GetSalaryHistoryDto>> GetSalaryHistoryById(int id);
    public Task<Response<bool>> DeleteSalaryHistory(int id);

}