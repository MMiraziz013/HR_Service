using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports.ReportFilters;
using Clean.Application.Dtos.Reports.VacationBalance;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class VacationBalanceRepository : IVacationBalanceRepository
{
    private readonly DataContext _context;

    public VacationBalanceRepository(DataContext context)
    {
        _context = context;
    }
    
    public async Task<bool> AddVacationBalanceAsync(VacationBalance dto)
    {
        await _context.VacationBalances.AddAsync(dto);
        var isAdded = await _context.SaveChangesAsync();
        return isAdded > 0;
    }

    public async Task<List<VacationBalance>> GetVacationBalancesAsync(VacationBalanceFilter filter)
    {
        var query =  _context.VacationBalances
            .Include(vb => vb.Employee)
            .ThenInclude(e=> e.Department)
            .Include(vb=> vb.Employee)
            .ThenInclude(e=> e.SalaryHistories)
            .AsQueryable();

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(vb => vb.EmployeeId == filter.EmployeeId);
        }
        
        if (filter.UserRole.HasValue)
        {
            query = query.Where(vb => vb.Employee.User.Role == filter.UserRole);
        }

        if (filter.EmployeePosition.HasValue)
        {
            query = query.Where(vb => vb.Employee.Position == filter.EmployeePosition);
        }
        if (filter.Year.HasValue)
        {
            query = query.Where(vb => vb.Year == filter.Year);
        }
        
        return await query.ToListAsync();
    }

    public async Task<VacationBalance?> GetVacationBalanceByIdAsync(int vacationBalanceId)
    {
        var vacationBalance = await _context.VacationBalances
            .Include(vb=> vb.Employee)
            .ThenInclude(e=> e.Department)
            .Include(vb=> vb.Employee)
            .ThenInclude(e=> e.SalaryHistories)
            .FirstOrDefaultAsync(vb => vb.Id == vacationBalanceId);
        return vacationBalance;
    }

    public async Task<VacationBalance?> UpdateVacationBalanceAsync(VacationBalance dto)
    {
        var toUpdate = await _context.VacationBalances
            .Include(vb=> vb.Employee)
            .ThenInclude(e=> e.Department)
            .Include(vb=> vb.Employee)
            .ThenInclude(e=> e.SalaryHistories)
            .FirstOrDefaultAsync(vb => vb.Id == dto.Id);
        
        
        if (toUpdate is null)
        {
            return null;
        }

        _context.Entry(toUpdate).CurrentValues.SetValues(dto);
        await _context.SaveChangesAsync();

        return toUpdate;
    }

    // public async Task<int> GetEmployeeRemainingDaysAsync(int employeeId)
    // {
    //     var days = await _context.VacationBalances
    //         .Where(vb => vb.EmployeeId == employeeId)
    //         .Select(vb => vb.RemainingDays)
    //         .FirstOrDefaultAsync();
    //     return days;
    // }

    public async Task<VacationBalance?> GetVacationBalanceByEmployeeIdAsync(int employeeId)
    {
        var balance = await _context.VacationBalances
            .Where(vb => vb.EmployeeId == employeeId)
            .Include(vb => vb.Employee)
            .OrderByDescending(vb => vb.PeriodEnd)
            .FirstOrDefaultAsync();

        return balance;
    }
    

    public async Task<bool> ExistsAsync(int employeeId, int year)
    {
        return await _context.VacationBalances
            .AnyAsync(v => v.EmployeeId == employeeId && v.Year == year);
    }

    public async Task<List<VacationBalanceDto>> GetVacationBalanceReportAsync(VacationBalanceReportFilter filter)
    {
        var query = _context.VacationBalances
            .Include(vb => vb.Employee)
            .ThenInclude(e => e.Department)
            .Include(vacationBalance => vacationBalance.Employee)
            .ThenInclude(employee => employee.VacationRecords)
            .Include(vacationBalance => vacationBalance.Employee)
            .ThenInclude(employee => employee.User)
            .AsQueryable();

        if (filter.Year.HasValue)
        {
            query = query.Where(v => v.Year == filter.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.DepartmentName))
        {
            query = query.Where(v => v.Employee.Department.Name
                .ToLower().Contains(filter.DepartmentName.ToLower()));        }

        if (!string.IsNullOrWhiteSpace(filter.ByEmployeeName))
        {
            var nameParts = filter.ByEmployeeName
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

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (filter.MinWorkedYears.HasValue)
        {
            var minHireDate = today.AddYears(-filter.MinWorkedYears.Value);
            query = query.Where(vb => vb.Employee.HireDate <= minHireDate);
        }

        if (filter.MaxWorkedYears.HasValue)
        {
            var maxHireDate = today.AddYears(-filter.MaxWorkedYears.Value);
            query = query.Where(vb => vb.Employee.HireDate >= maxHireDate);
        }


        if (filter.HasBonusDays.HasValue)
        {
            query = query.Where(v => filter.HasBonusDays.Value
                ? v.ByExperienceBonusDays > 0
                : v.ByExperienceBonusDays == 0);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(v => v.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Position.HasValue)
        {
            query = query.Where(v => v.Employee.Position == filter.Position);
        }

        if (filter.FromPeriodStart.HasValue)
        {
            query = query.Where(v => v.PeriodStart >= filter.FromPeriodStart.Value);
        }

        if (filter.ToPeriodEnd.HasValue)
        {
            query = query.Where(v => v.PeriodEnd <= filter.ToPeriodEnd.Value);
        }

        if (filter.MinUsedDays.HasValue)
        {
            query = query.Where(v => v.UsedDays >= filter.MinUsedDays.Value);
        }

        if (filter.MaxUsedDays.HasValue)
        {
            query = query.Where(v => v.UsedDays <= filter.MaxUsedDays.Value);
        }

        if (filter.MinRemainingDays.HasValue)
        {
            query = query.Where(v => (v.TotalDaysPerYear - v.UsedDays) >= filter.MinRemainingDays.Value);
        }

        if (filter.MaxRemainingDays.HasValue)
        {
            query = query.Where(v => (v.TotalDaysPerYear - v.UsedDays) <= filter.MaxRemainingDays.Value);
        }

        if (filter.IsLimitFinished.HasValue)
        {
            query = query.Where(v => filter.IsLimitFinished.Value
                ? (v.TotalDaysPerYear - v.UsedDays) == 0
                : (v.TotalDaysPerYear - v.UsedDays) > 0);

        }

        if (filter.HasUsedDays.HasValue)
        {
            query = query.Where(v => filter.HasUsedDays.Value
                ? v.UsedDays > 0
                : v.UsedDays == 0);

        }

        var list = await query.ToListAsync();
        var today2 = DateOnly.FromDateTime(DateTime.Today);

        var balances = list.Select(vb => new VacationBalanceDto
        {
            Id = vb.Id,
            EmployeeId = vb.EmployeeId,
            EmployeeName = vb.Employee.FirstName + " " + vb.Employee.LastName,
            DepartmentName = vb.Employee.Department.Name,
            Position = vb.Employee.Position.ToString(),
            Role = vb.Employee.User.Role.ToString(),

            WorkedYears = today2.Year - vb.Employee.HireDate.Year -
                          (today2.DayOfYear < vb.Employee.HireDate.DayOfYear ? 1 : 0),

            ByExperienceBonusDays = vb.ByExperienceBonusDays,
            TotalDaysPerYear = vb.TotalDaysPerYear,
            UsedDays = vb.UsedDays,
            VacationsTaken = vb.Employee.VacationRecords.Count(vr => vr.StartDate.Year == DateTime.Today.Year),

            Year = vb.Year,
            BalanceFrom = vb.PeriodStart,
            BalanceTo = vb.PeriodEnd
        }).ToList();

        return balances;
    }
}