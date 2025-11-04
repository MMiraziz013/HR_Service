using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationRecords;
using Clean.Domain.Enums;

namespace Clean.Application.Services.VacationRecord;

public class VacationRecordService : IVacationRecordService
{
    private readonly IVacationRecordRepository _vacationRecordRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public VacationRecordService(IVacationRecordRepository vacationRecordRepository, IEmployeeRepository employeeRepository)
    {
        _vacationRecordRepository = vacationRecordRepository;
        _employeeRepository = employeeRepository;
    }
    
    public async Task<Response<GetVacationRecordDto>> AddVacationRecordAsync(AddVacationRecordDto dto)
    {
        var vacationToAdd = new Domain.Entities.VacationRecord
        {
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Type = dto.Type,
            Status = VacationStatus.Pending,
            // TODO: later here call the function that automatically calculates
            // the payment amount depending on the number of days, experience, etc.
            PaymentAmount = 100,
            ManagerComment = null
        };

        var addedRecord = await _vacationRecordRepository.AddAsync(vacationToAdd);
        if (addedRecord is null)
        {
            return new Response<GetVacationRecordDto>(HttpStatusCode.InternalServerError,
                "Error while creating the new vacation record");
        }

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
                BaseSalary = employee.SalaryHistories.OrderByDescending(sh=> sh.Month).Select(sh=> sh.BaseAmount).FirstOrDefault(),
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

    public async Task<PaginatedResponse<GetVacationRecordDto>> GetVacationRecordsAsync(VacationRecordPaginationFilter filter)
    {
        var (vacationRecords, totalRecords) = await _vacationRecordRepository.GetAllAsync(filter);
        return new PaginatedResponse<GetVacationRecordDto>(vacationRecords, filter.PageNumber, filter.PageSize, totalRecords);
    }

    public async Task<Response<GetVacationRecordDto>> GetVacationRecordByIdAsync(int id)
    {
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

        return new Response<GetVacationRecordDto>(HttpStatusCode.OK, vacationRecord);
    }

    public async Task<Response<List<VacationSummaryDto>>> GetVacationSummaryForLastFiveMonthsAsync()
    {
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

        return new Response<List<VacationSummaryDto>>(HttpStatusCode.OK, summary);
    }


    public async Task<Response<VacationCheckDto>> CheckVacationAvailabilityAsync(RequestVacationDto dto)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
        var isAvailable = VacationRecordChecker.CheckVacation(dto, employee!);
        
        if (isAvailable.IsAvailable == false)
        {
            return new Response<VacationCheckDto>(HttpStatusCode.BadRequest, message:isAvailable.Message!,isAvailable);
        }
        return new Response<VacationCheckDto>(HttpStatusCode.OK, isAvailable);
    }

    public async Task<Response<string>> SendVacationRequestAsync(GetVacationRecordDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task<Response<GetVacationRecordDto>> UpdateVacationRecordAsync(UpdateVacationRecordDto dto)
    {
        var record = await _vacationRecordRepository.GetByIdAsync(dto.Id);
        if (record is null)
        {
            return new Response<GetVacationRecordDto>(HttpStatusCode.NotFound, "No such vacation record to update");
        }
        
        if (dto.EmployeeId.HasValue)
        {
            record.EmployeeId = dto.EmployeeId.Value;
        }

        if (dto.StartDate.HasValue)
        {
            record.StartDate = dto.StartDate.Value;
        }

        if (dto.EndDate.HasValue)
        {
            record.EndDate = dto.EndDate.Value;
        }

        if (dto.Status.HasValue)
        {
            record.Status = dto.Status.Value;
        }

        if (dto.Type.HasValue)
        {
            record.Type = dto.Type.Value;
        }
        
        //TODO: See if it should be calculated automatically rather than set manually.
        if (dto.PaymentAmount.HasValue) 
        {
            record.PaymentAmount = dto.PaymentAmount;
        }

        var updatedRecord = await _vacationRecordRepository.UpdateAsync(record);
        if (updatedRecord is null)
        {
            return new Response<GetVacationRecordDto>(HttpStatusCode.InternalServerError,
                "Error while updating the record");
        }
        var employee = await _employeeRepository.GetEmployeeByIdAsync(updatedRecord.EmployeeId);

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

        return new Response<GetVacationRecordDto>(HttpStatusCode.OK, vacationRecord);
    }

    public async Task<Response<bool>> DeleteVacationRecordAsync(int id)
    {
        var isDeleted = await _vacationRecordRepository.Delete(id);
        if (isDeleted == false)
        {
            return new Response<bool>(HttpStatusCode.NotFound, "No such record to delete");
        }

        return new Response<bool>(HttpStatusCode.OK, isDeleted);
    }

    public Task<Response<bool>> HrCheckVacationRequestAsync(VacationRecordHrResponseDto dto)
    {
        throw new NotImplementedException();
    }
}