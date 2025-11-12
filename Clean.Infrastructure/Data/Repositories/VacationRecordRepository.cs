using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports.ReportFilters;
using Clean.Application.Dtos.Reports.VacationRecord;
using Clean.Application.Dtos.VacationRecords;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class VacationRecordRepository : IVacationRecordRepository
{
    private readonly DataContext _context;

    public VacationRecordRepository(DataContext context)
    {
        _context = context;
    }
    
    public async Task<VacationRecord?> AddAsync(VacationRecord vacationRecord)
    {
        await _context.VacationRecords.AddAsync(vacationRecord);
        var isAdded = await _context.SaveChangesAsync();
        return vacationRecord;

    }

    public async Task<(List<GetVacationRecordDto> vacationRecord, int totalRecords)> GetAllAsync(VacationRecordPaginationFilter filter)
    {
        var query = _context.VacationRecords
            .Include(record => record.Employee)
            .ThenInclude(e=> e.SalaryHistories)
            .Include(vr=> vr.Employee)
            .ThenInclude(e=> e.Department)
            .AsQueryable();

        if (filter.Id.HasValue)
        {
            query = query.Where(record => record.Id == filter.Id);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(record => record.EmployeeId == filter.EmployeeId);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(record => record.StartDate >= filter.StartDate);
        }
        
        if (filter.EndDate.HasValue)
        {
            query = query.Where(record => record.EndDate <= filter.EndDate);
        }

        if (filter.VacationStatus.HasValue)
        {
            query = query.Where(record => record.Status == filter.VacationStatus);
        }
        
        var totalRecords = await query.CountAsync();

        query = query
            .OrderBy(vr => vr.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);

        var filteredVacationRecords = await query.Select(vr => new GetVacationRecordDto
        {
            Id = vr.Id,
            EmployeeId = vr.EmployeeId,
            Employee = new GetEmployeeDto
            {
                Id = vr.EmployeeId,
                FirstName = vr.Employee.FirstName,
                LastName = vr.Employee.LastName,
                Position = vr.Employee.Position,
                HireDate = vr.Employee.HireDate.ToString("yyyy-MM-dd"),
                BaseSalary = vr.Employee.SalaryHistories.OrderByDescending(sh=> sh.Month).Select(sh=> sh.BaseAmount).FirstOrDefault(),
                IsActive = vr.Employee.IsActive,
                DepartmentName = vr.Employee.Department.Name
            },
            StartDate = vr.StartDate.ToString("yyyy-MM-dd"),
            EndDate = vr.EndDate.ToString("yyyy-MM-dd"),
            Type = vr.Type,
            Status = vr.Status,
            DaysCount = vr.DaysCount,
            PaymentAmount = vr.PaymentAmount
        }).ToListAsync();
        
        return (filteredVacationRecords, totalRecords);
    }

    public async Task<List<VacationRecord>> GetAllBetweenDatesAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _context.VacationRecords
            .Include(v => v.Employee)
            .Where(v =>
                v.StartDate <= endDate &&
                v.EndDate >= startDate && v.Status != VacationStatus.Rejected)
            .ToListAsync();
    }

    public async Task<VacationRecord?> GetByIdAsync(int id)
    {
        var record = await _context.VacationRecords
            .Include(vr=> vr.Employee)
            .ThenInclude(e=>e.Department)
            .Include(vr=> vr.Employee)
            .ThenInclude(e=> e.SalaryHistories)
            .FirstOrDefaultAsync(vr => vr.Id == id);
        return record;
    }

    public async Task<List<VacationRecord>> GetByAllUserIdAsync(int userId)
    {
        var record = await _context.VacationRecords
            .Include(vr=> vr.Employee)
            .ThenInclude(e=>e.Department)
            .Include(vr=> vr.Employee)
            .ThenInclude(e=> e.SalaryHistories)
            .Where(vr=> vr.Employee.UserId == userId)
            .ToListAsync();
        return record;
    }

    public async Task<VacationRecord?> UpdateAsync(VacationRecord recordToUpdate)
    {
        var existing = await _context.VacationRecords
            .Include(vr => vr.Employee)
            .ThenInclude(e => e.Department)
            .Include(vr => vr.Employee)
            .ThenInclude(e => e.SalaryHistories)
            .FirstOrDefaultAsync(vr => vr.Id == recordToUpdate.Id);

        if (existing == null)
            return null;

        _context.Entry(existing).CurrentValues.SetValues(recordToUpdate);
        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> Delete(int id)
    {
        var exists = await GetByIdAsync(id);
        if (exists is null)
        {
            return false;
        }

        _context.VacationRecords.Remove(exists);
        var isDeleted = await _context.SaveChangesAsync();
        return isDeleted > 0;
    }
    
    public async Task<List<VacationRecord>> GetVacationsToFinishAsync(DateOnly today)
    {
        return await _context.VacationRecords
            .Where(v => v.Status == VacationStatus.Approved && v.EndDate < today)
            .ToListAsync();
    }

    public async Task<List<VacationRecordDto>> GetVacationRecordReportAsync(VacationRecordReportFilter filter)
    {
        var query = _context.VacationRecords
            .Include(v => v.Employee)
            .ThenInclude(e => e.Department)
            .Include(v => v.Employee)
            .ThenInclude(e => e.User)
            .AsQueryable();

        if (filter.Id.HasValue)
        {
            query = query.Where(v => v.Id == filter.Id.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(v => v.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(v => v.Employee.User.Role == filter.Role.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.EmployeeName))
        {
            var nameParts = filter.EmployeeName
                .Trim()
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 1)
            {
                var name = nameParts[0];
                query = query.Where(v =>
                    v.Employee.FirstName.ToLower().Contains(name) ||
                    v.Employee.LastName.ToLower().Contains(name));
            }
            else if (nameParts.Length >= 2)
            {
                var firstName = nameParts[0];
                var lastName = nameParts[1];
                query = query.Where(v =>
                    v.Employee.FirstName.ToLower().Contains(firstName) &&
                    v.Employee.LastName.ToLower().Contains(lastName));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.DepartmentName))
        {
            query = query.Where(v =>
                v.Employee.Department.Name.ToLower().Contains(filter.DepartmentName.ToLower()));
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(v =>
                v.StartDate.Year == filter.Year.Value || v.EndDate.Year == filter.Year.Value);
        }

        if (filter.VacationType.HasValue)
        {
            query = query.Where(v => v.Type == filter.VacationType.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(v => v.Status == filter.Status.Value);
        }

        if (filter.MinDuration.HasValue)
        {
            query = query.Where(v => (v.EndDate.DayNumber - v.StartDate.DayNumber + 1) >= filter.MinDuration.Value);
        }

        if (filter.MaxDuration.HasValue)
        {
            query = query.Where(v => (v.EndDate.DayNumber - v.StartDate.DayNumber + 1) <= filter.MaxDuration.Value);
        }

        if (filter.MinPaymentAmount.HasValue)
        {
            query = query.Where(v => v.PaymentAmount.HasValue && v.PaymentAmount.Value >= filter.MinPaymentAmount.Value);
        }

        if (filter.MaxPaymentAmount.HasValue)
        {
            query = query.Where(v => v.PaymentAmount.HasValue && v.PaymentAmount.Value <= filter.MaxPaymentAmount.Value);
        }

        if (filter.IsCurrentlyActive.HasValue && filter.IsCurrentlyActive.Value)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(v => v.StartDate <= today && v.EndDate >= today);
        }

        // Filter by StartDate range
        if (filter.StartDateFrom.HasValue)
        {
            query = query.Where(v => v.StartDate >= filter.StartDateFrom.Value);
        }

        if (filter.StartDateTo.HasValue)
        {
            query = query.Where(v => v.StartDate <= filter.StartDateTo.Value);
        }

        // Filter by EndDate range
        if (filter.EndDateFrom.HasValue)
        {
            query = query.Where(v => v.EndDate >= filter.EndDateFrom.Value);
        }

        if (filter.EndDateTo.HasValue)
        {
            query = query.Where(v => v.EndDate <= filter.EndDateTo.Value);
        }

        var list = await query
            .Select(v => new VacationRecordDto
            {
                Id = v.Id,
                EmployeeId = v.EmployeeId,
                EmployeeName = v.Employee.FirstName + " " + v.Employee.LastName,
                DepartmentName = v.Employee.Department.Name,
                VacationDays = v.EndDate.DayNumber - v.StartDate.DayNumber + 1,
                VacationType = v.Type.ToString(),
                PaidAmount = v.PaymentAmount ?? 0,
                Status = v.Status.ToString(),
                From = v.StartDate,
                To = v.EndDate
            })
            .ToListAsync();

        return list;
    }

}