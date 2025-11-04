using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
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
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            return null;
        }

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
}