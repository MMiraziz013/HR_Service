using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationRecords;

namespace Clean.Application.Abstractions;

public interface IVacationRecordService
{
    Task<Response<GetVacationRecordDto>> AddVacationRecordAsync(AddVacationRecordDto dto);
    Task<PaginatedResponse<GetVacationRecordDto>> GetVacationRecordsAsync(VacationRecordPaginationFilter filter);

    Task<Response<GetVacationRecordDto>> GetVacationRecordByIdAsync(int id);

    Task<Response<List<VacationSummaryDto>>> GetVacationSummaryForLastFiveMonthsAsync();
    
    Task<Response<VacationCheckDto>> CheckVacationAvailabilityAsync(RequestVacationDto dto);

    Task<Response<string>> SendVacationRequestAsync(GetVacationRecordDto dto);
    
    Task<Response<GetVacationRecordDto>> UpdateVacationRecordAsync(UpdateVacationRecordDto dto);

    Task<Response<bool>> DeleteVacationRecordAsync(int id);

    Task<Response<bool>> HrRespondToVacationRequest(VacationRecordHrResponseDto dto);
}