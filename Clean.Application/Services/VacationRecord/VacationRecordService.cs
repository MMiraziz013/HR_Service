using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationRecords;
using Clean.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.VacationRecord;

public class VacationRecordService : IVacationRecordService
{
    private readonly IVacationRecordRepository _vacationRecordRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<VacationRecordService> _logger;
    private readonly IEmailService _emailService;

    public VacationRecordService(
        IVacationRecordRepository vacationRecordRepository, 
        IEmployeeRepository employeeRepository,
        IDataContext context,
        ICacheService cacheService,
        ILogger<VacationRecordService> logger,
        IEmailService emailService)
    {
        _vacationRecordRepository = vacationRecordRepository;
        _employeeRepository = employeeRepository;
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
        _emailService = emailService;
    }
    
    
    /// <summary>
    /// Automatically marks vacations that have ended as Finished.
    /// </summary>
    public async Task AutoUpdateVacationStatusesAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var vacationsToFinish = await _vacationRecordRepository.GetVacationsToFinishAsync(today);

            if (vacationsToFinish.Count == 0)
            {
                _logger.LogInformation("No vacation records to finish as of {date}.", today);
                return;
            }

            foreach (var vacation in vacationsToFinish.Where(v => v.Status != VacationStatus.Finished))
            {
                vacation.Status = VacationStatus.Finished;
                await _cacheService.RemoveAsync($"vacation_record_{vacation.Id}");
            }

            await _context.SaveChangesAsync(CancellationToken.None);
            await InvalidateVacationRecordListsCache();

            _logger.LogInformation("Marked {count} vacation records as Finished.", vacationsToFinish.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while auto-updating vacation statuses.");
            throw;
        }
    }
    
    public async Task<Response<GetVacationRecordDto>> AddVacationRecordAsync(AddVacationRecordDto dto)
    {
        try
        {
            var vacationToAdd = new Domain.Entities.VacationRecord
            {
                EmployeeId = dto.EmployeeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Type = dto.Type,
                Status = VacationStatus.Pending,
                PaymentAmount = 100,
                ManagerComment = null
            };

            var addedRecord = await _vacationRecordRepository.AddAsync(vacationToAdd);
            if (addedRecord is null)
            {
                return new Response<GetVacationRecordDto>(HttpStatusCode.InternalServerError,
                    "Error while creating the new vacation record");
            }

            await InvalidateVacationRecordListsCache();

            var employee = await _employeeRepository.GetEmployeeByIdAsync(addedRecord.EmployeeId);

            var addedVacation = new GetVacationRecordDto
            {
                Id = addedRecord.Id,
                EmployeeId = addedRecord.EmployeeId,
                Employee = new GetEmployeeDto
                {
                    Id = employee!.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Position = employee.Position,
                    HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
                    BaseSalary = employee.SalaryHistories.OrderByDescending(sh => sh.Month)
                        .Select(sh => sh.BaseAmount).FirstOrDefault(),
                    IsActive = employee.IsActive,
                    DepartmentName = employee.Department.Name
                },
                StartDate = addedRecord.StartDate.ToString("yyyy-MM-dd"),
                EndDate = addedRecord.EndDate.ToString("yyyy-MM-dd"),
                Type = addedRecord.Type,
                Status = addedRecord.Status,
                DaysCount = addedRecord.DaysCount,
                PaymentAmount = addedRecord.PaymentAmount
            };

            return new Response<GetVacationRecordDto>(HttpStatusCode.OK, addedVacation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding a new vacation record.");
            return new Response<GetVacationRecordDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while adding vacation record.");
        }
    }

    public async Task<PaginatedResponse<GetVacationRecordDto>> GetVacationRecordsAsync(VacationRecordPaginationFilter filter)
    {
        try
        {
            var (vacationRecords, totalRecords) = await _vacationRecordRepository.GetAllAsync(filter);
            return new PaginatedResponse<GetVacationRecordDto>(vacationRecords, filter.PageNumber, filter.PageSize, totalRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving vacation records.");
            return new PaginatedResponse<GetVacationRecordDto>([], filter.PageNumber, filter.PageSize, 0);
        }
    }

    public async Task<Response<GetVacationRecordDto>> GetVacationRecordByIdAsync(int id)
    {
         try
         {
             var cacheKey = $"vacation_record_{id}";
             var cached = await _cacheService.GetAsync<GetVacationRecordDto>(cacheKey);
             if (cached != null)
             {
                 return new Response<GetVacationRecordDto>(HttpStatusCode.OK, "Vacation record retrieved successfully!", cached);
             }

             var record = await _vacationRecordRepository.GetByIdAsync(id);
             if (record is null)
             {
                 return new Response<GetVacationRecordDto>(HttpStatusCode.NotFound, "No such vacation record in the system");
             }

             var employee = await _employeeRepository.GetEmployeeByIdAsync(record.EmployeeId);

             var vacationRecord = new GetVacationRecordDto
             {
                 Id = record.Id,
                 EmployeeId = record.EmployeeId,
                 Employee = new GetEmployeeDto
                 {
                     Id = employee!.Id,
                     FirstName = employee.FirstName,
                     LastName = employee.LastName,
                     Position = employee.Position,
                     HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
                     BaseSalary = employee.SalaryHistories.OrderByDescending(sh => sh.Month).Select(sh => sh.BaseAmount)
                         .FirstOrDefault(),
                     IsActive = employee.IsActive,
                     DepartmentName = employee.Department.Name
                 },
                 StartDate = record.StartDate.ToString("yyyy-MM-dd"),
                 EndDate = record.EndDate.ToString("yyyy-MM-dd"),
                 Type = record.Type,
                 Status = record.Status,
                 DaysCount = record.DaysCount,
                 PaymentAmount = record.PaymentAmount
             };

             await _cacheService.SetAsync(cacheKey, vacationRecord, TimeSpan.FromHours(1));
             return new Response<GetVacationRecordDto>(HttpStatusCode.OK, vacationRecord);
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Error while retrieving vacation record with ID {id}", id);
             return new Response<GetVacationRecordDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving vacation record.");
         }
    }

    public async Task<Response<List<VacationSummaryDto>>> GetVacationSummaryForLastFiveMonthsAsync()
    {
        try
        {
            const string cacheKey = "vacation_summary_last_five_months";
            var cached = await _cacheService.GetAsync<List<VacationSummaryDto>>(cacheKey);
            if (cached != null)
            {
                return new Response<List<VacationSummaryDto>>(HttpStatusCode.OK, "Vacation summary retrieved successfully!", cached);
            }

            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = new DateOnly(now.Year, now.Month, 1).AddMonths(-4);
            var end = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            var vacationRecords = await _vacationRecordRepository.GetAllBetweenDatesAsync(start, end);

            var summary = vacationRecords
                .GroupBy(v => new { v.StartDate.Year, v.StartDate.Month })
                .Select(g => new VacationSummaryDto
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalVacationDays = g.Sum(v => v.DaysCount),
                    EmployeesOnVacation = g.Select(v => v.EmployeeId).Distinct().Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromHours(6));
            return new Response<List<VacationSummaryDto>>(HttpStatusCode.OK, summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving vacation summary for last five months.");
            return new Response<List<VacationSummaryDto>>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving vacation summary.");
        }

    }
    
    public async Task<Response<VacationCheckDto>> CheckVacationAvailabilityAsync(RequestVacationDto dto)
    {
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
            var isAvailable = VacationRecordChecker.CheckVacation(dto, employee!);

            if (isAvailable.IsAvailable == false)
            {
                return new Response<VacationCheckDto>(HttpStatusCode.BadRequest, message: isAvailable.Message!, isAvailable);
            }
            return new Response<VacationCheckDto>(HttpStatusCode.OK, isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking vacation availability.");
            return new Response<VacationCheckDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while checking vacation availability.");
        }
    }

    public async Task<Response<string>> SendVacationRequestAsync(GetVacationRecordDto dto)
    {
        try
        {
            // 1️⃣ Get the employee who made the request
            var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
            if (employee == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Employee not found.");
            }

            // 2️⃣ Get all HR + Senior employees
            var hrEmployees = await _employeeRepository.GetActiveEmployeesAsync(); // adjust if you have a specific method
            var hrUsers = hrEmployees
                .Where(e => e.User.Role == UserRole.HrManager && e.Position == EmployeePosition.Senior && e.IsActive)
                .ToList();

            if (!hrUsers.Any())
            {
                return new Response<string>(HttpStatusCode.NotFound, "No HR employees with senior position found.");
            }

            // 3️⃣ Send email to each HR user
            foreach (var hrEmail in hrUsers.Select(hr => hr.User.Email))
            {
                await _emailService.SendVacationRequestEmailAsync(
                    vacationRequestId: dto.Id,
                    hrEmail: hrEmail!,
                    employeeName: $"{employee.FirstName} {employee.LastName}",
                    fromDate: dto.StartDate,
                    toDate: dto.EndDate
                );
            }

            return new Response<string>(HttpStatusCode.OK, "Vacation request email(s) sent successfully.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Postmark API error while sending vacation request email.");
            return new Response<string>(HttpStatusCode.BadGateway, "Email service connection failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending vacation request email.");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected error occurred while sending vacation request.");
        }
    }

    public async Task<Response<bool>> CancelVacationRequestAsync(int vacationId)
    {
        try
        {
            var requestToDelete = await _vacationRecordRepository.GetByIdAsync(vacationId);
            if (requestToDelete is null)
            {
                return new Response<bool>(HttpStatusCode.BadRequest, "No such vacation request to cancel", false);
            }

            if (requestToDelete.Status != VacationStatus.Pending)
            {
                return new Response<bool>(HttpStatusCode.BadRequest, "Only pending requests can be cancelled");
            }

            requestToDelete.Status = VacationStatus.Cancelled;
            var isCancelled = await _vacationRecordRepository.UpdateAsync(requestToDelete);
            if (isCancelled is null)
            {
                return new Response<bool>(HttpStatusCode.InternalServerError, "Error while cancelling the request", false);
            }

            await _cacheService.RemoveAsync($"vacation_record_{vacationId}");
            await InvalidateVacationRecordListsCache();

            return new Response<bool>(HttpStatusCode.OK, "Request successfully cancelled!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while cancelling vacation request with ID {vacationId}", vacationId);
            return new Response<bool>(HttpStatusCode.InternalServerError, "Unexpected error occurred while cancelling vacation request.");
        }

    }

    // public async Task<Response<VacationCheckDto>> UpdateVacationRecordAsync(UpdateVacationRecordDto dto)
    // {
    //     var record = await _vacationRecordRepository.GetByIdAsync(dto.Id);
    //     if (record is null)
    //     {
    //         return new Response<GetVacationRecordDto>(HttpStatusCode.NotFound, "No such vacation record to update");
    //     }
    //     
    //     if (dto.StartDate.HasValue)
    //     {
    //         record.StartDate = dto.StartDate.Value;
    //     }
    //
    //     if (dto.EndDate.HasValue)
    //     {
    //         record.EndDate = dto.EndDate.Value;
    //     }
    //
    //     if (dto.Status.HasValue)
    //     {
    //         record.Status = dto.Status.Value;
    //     }
    //
    //     if (dto.Type.HasValue)
    //     {
    //         record.Type = dto.Type.Value;
    //     }
    //     
    //     //TODO: See if it should be calculated automatically rather than set manually.
    //     if (dto.PaymentAmount.HasValue) 
    //     {
    //         record.PaymentAmount = dto.PaymentAmount;
    //     }
    //
    //     var isPossible = await CheckVacationAvailabilityAsync(record);
    //
    //     var updatedRecord = await _vacationRecordRepository.UpdateAsync(record);
    //     if (updatedRecord is null)
    //     {
    //         return new Response<GetVacationRecordDto>(HttpStatusCode.InternalServerError,
    //             "Error while updating the record");
    //     }
    //     
    //     await _cacheService.RemoveAsync($"vacation_record_{updatedRecord.Id}");
    //     await InvalidateVacationRecordListsCache();
    //     var employee = await _employeeRepository.GetEmployeeByIdAsync(updatedRecord.EmployeeId);
    //
    //     var vacationRecord = new GetVacationRecordDto
    //     {
    //         Id = record.Id,
    //         EmployeeId = record.EmployeeId,
    //         Employee = new GetEmployeeDto
    //         {
    //             Id = employee!.Id,
    //             FirstName = employee.FirstName,
    //             LastName = employee.LastName,
    //             Position = employee.Position,
    //             HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
    //             BaseSalary = employee.SalaryHistories.OrderByDescending(sh => sh.Month).Select(sh => sh.BaseAmount)
    //                 .FirstOrDefault(),
    //             IsActive = employee.IsActive,
    //             DepartmentName = employee.Department.Name
    //         },
    //         StartDate = record.StartDate.ToString("yyyy-MM-dd"),
    //         EndDate = record.EndDate.ToString("yyyy-MM-dd"),
    //         Type = record.Type,
    //         Status = record.Status,
    //         DaysCount = record.DaysCount,
    //         PaymentAmount = record.PaymentAmount
    //     };
    //
    //     return new Response<GetVacationRecordDto>(HttpStatusCode.OK, vacationRecord);
    // }

    public async Task<Response<bool>> DeleteVacationRecordAsync(int id)
    {
        try
        {
            var isDeleted = await _vacationRecordRepository.Delete(id);
            if (isDeleted == false)
            {
                return new Response<bool>(HttpStatusCode.NotFound, "No such record to delete");
            }

            await _cacheService.RemoveAsync($"vacation_record_{id}");
            await InvalidateVacationRecordListsCache();

            return new Response<bool>(HttpStatusCode.OK, isDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting vacation record with ID {id}", id);
            return new Response<bool>(HttpStatusCode.InternalServerError, "Unexpected error occurred while deleting vacation record.");
        }
    }

    public async Task<Response<bool>> HrRespondToVacationRequest(VacationRecordHrResponseDto dto)
    {
        try
        {
            var vacationRequest = await _vacationRecordRepository.GetByIdAsync(dto.Id);
            if (vacationRequest is null)
            {
                return new Response<bool>(HttpStatusCode.NotFound, "No such vacation request to accept", false);
            }

            if (dto.UpdatedStatus is null)
            {
                return new Response<bool>(HttpStatusCode.BadRequest, "UpdatedStatus is required.", false);
            }

            if (string.IsNullOrEmpty(dto.Comment) == false)
            {
                vacationRequest.ManagerComment = dto.Comment;
            }

            switch (vacationRequest.Status)
            {
                case VacationStatus.Pending:
                    var shouldInvalidate = false;

                    if (dto.UpdatedStatus == VacationStatus.Approved)
                    {
                        shouldInvalidate = true;
                        vacationRequest.Status = VacationStatus.Approved;
                        await _vacationRecordRepository.UpdateAsync(vacationRequest);
                        return new Response<bool>(HttpStatusCode.OK, "Vacation request approved successfully.", true);
                    }

                    vacationRequest.Status = VacationStatus.Rejected;
                    shouldInvalidate = true;
                    await _vacationRecordRepository.UpdateAsync(vacationRequest);

                    if (shouldInvalidate)
                    {
                        await _cacheService.RemoveAsync($"vacation_record_{vacationRequest.Id}");
                        await InvalidateVacationRecordListsCache();
                    }

                    return new Response<bool>(HttpStatusCode.OK, "Vacation request rejected successfully.", true);

                case VacationStatus.Rejected:
                    return new Response<bool>(HttpStatusCode.BadRequest, "This vacation was already rejected!", false);

                case VacationStatus.Finished:
                    return new Response<bool>(HttpStatusCode.BadRequest, "This vacation already finished!", false);

                case VacationStatus.Cancelled:
                    return new Response<bool>(HttpStatusCode.BadRequest, "This vacation request was cancelled by employee",
                        false);

                case VacationStatus.Approved:
                default:
                    return new Response<bool>(HttpStatusCode.BadRequest, "This vacation already approved!", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while HR responding to vacation request.");
            return new Response<bool>(HttpStatusCode.InternalServerError, "Unexpected error occurred while processing HR vacation response.");
        }
    }

    private async Task InvalidateVacationRecordListsCache()
    {
        try
        {
            await _cacheService.RemoveAsync("vacation_summary_last_five_months");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while invalidating vacation record cache.");
        }
    }
}