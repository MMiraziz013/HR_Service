using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryAnomaly;

namespace Clean.Application.Abstractions;

public interface ISalaryAnomalyService
{
  
    Task<Response<int>> GenerateAnomaliesAsync();
    Task<PaginatedResponse<GetSalaryAnomalyDto>> GetAllAsync();
    Task<Response<List<GetSalaryAnomalyDto>>> GetUnviewedAsync();
    Task<Response<GetSalaryAnomalyDto>> MarkAsViewedAsync(int id);
    Task<Response<bool>> DeleteAsync(int id);
    Task<Response<GetSalaryAnomalyDto>> AddReviewCommentAsync(int employeeId, string reviewComment);
    Task<Response<List<GetSalaryAnomalyDto>>> GetAnomalyByEmployeeId(int id);
    Task<PaginatedResponse<SalaryAnomalyListDto>> GetSalaryAnomaliesForListAsync();
}