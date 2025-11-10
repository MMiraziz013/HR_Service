using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationRecords;

namespace Clean.Application.Abstractions;

public interface IVacationRecordService
{
    Task AutoUpdateVacationStatusesAsync();
    
    Task<Response<GetVacationRecordDto>> AddVacationRecordAsync(AddVacationRecordDto dto);
    Task<PaginatedResponse<GetVacationRecordDto>> GetVacationRecordsAsync(VacationRecordPaginationFilter filter);

    Task<Response<GetVacationRecordDto>> GetVacationRecordByIdAsync(int id);

    Task<Response<List<VacationSummaryDto>>> GetVacationSummaryForLastFiveMonthsAsync();
    
    Task<Response<VacationCheckDto>> CheckVacationAvailabilityAsync(RequestVacationDto dto);

    Task<Response<string>> SubmitNewVacationRequestAsync(AddVacationRecordDto dto);
    
    // Task<Response<VacationCheckDto>> UpdateVacationRecordAsync(UpdateVacationRecordDto dto);

    Task<Response<bool>> CancelVacationRequestAsync(int vacationId);
    
    Task<Response<bool>> DeleteVacationRecordAsync(int id);

    Task<Response<bool>> HrRespondToVacationRequest(VacationRecordHrResponseDto dto);
}