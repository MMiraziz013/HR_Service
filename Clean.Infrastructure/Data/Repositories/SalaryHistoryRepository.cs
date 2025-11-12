using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports.SalaryHistory;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class SalaryHistoryRepository : ISalaryHistoryRepository
{

    private readonly DataContext _context;

    public SalaryHistoryRepository(DataContext context)
    {
        _context = context;
    }
    
    public async Task<bool> AddAsync(SalaryHistory entity)
    { 
        bool exists = await _context.SalaryHistories
            .AnyAsync(s => s.EmployeeId == entity.EmployeeId && s.Month == entity.Month);

        if (exists)
            return false;
        
        await _context.SalaryHistories.AddAsync(entity);
        var isAdded = await _context.SaveChangesAsync();
        return isAdded > 0;
    }


    public  async Task<List<SalaryHistory>> GetSalaryHistoryByEmployeeIdAsync(int employeeId)
    {
        return await _context.SalaryHistories
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.Month)
            .ToListAsync();
    }

    public async Task<List<SalaryHistory>> GetAllAsync()
    {
            return await  _context.SalaryHistories.ToListAsync();
    }

    public async Task<SalaryHistory?> GetByIdAsync(int id)
    {
        return await _context.SalaryHistories
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<SalaryHistory>> GetByMonthAsync(DateTime month)
    {
        return await _context.SalaryHistories
            .Include(s=> s.Employee)
            .Where(s => s.Month.Month == month.Month
                        && s.Month.Year == month.Year).ToListAsync();
    }

    public async Task<IEnumerable<SalaryHistoryDto>> GetForReportAsync(int? employeeId, int? departmentId, DateOnly? fromMonth,
        DateOnly? toMonth)
    {
        IQueryable<SalaryHistory> query = _context.SalaryHistories
            .Include(e => e.Employee)
            .Include(e => e.Employee.Department)
            .OrderBy(e => e.Id);

        if (employeeId.HasValue)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.Employee.DepartmentId == departmentId);
        }

        if (fromMonth.HasValue)
        {
            query = query.Where(e => e.Month >= fromMonth);
        }

        if (toMonth.HasValue)
        {
            query = query.Where(e => e.Month <= toMonth);
        }

        var histories = await query
            .Select(e => new SalaryHistoryDto
            {
                Id = e.Id,
                BaseAmount = e.BaseAmount,
                BonusAmount = e.BonusAmount,
                DepartmentId = e.Employee.DepartmentId,
                DepartmentName = e.Employee.Department.Name,
                EmployeeId = e.EmployeeId,
                EmployeeName = $"{e.Employee.FirstName} {e.Employee.LastName}",
                ExpectedTotal = e.ExpectedTotal,
                Month = e.Month
            }).ToListAsync();

        return histories;
    }
    public async Task<List<SalaryHistory>> GetSalaryHistoriesAsync(SalaryHistoryFilter filter)
    {
        var list = _context.SalaryHistories
            .Include(e => e.Employee)
            .OrderByDescending(s=>s.Month).AsQueryable();

        if (filter.EmployeeId.HasValue)
        {
            list = list.Where(s => s.EmployeeId == filter.EmployeeId);
        }

        if (filter.Year.HasValue)
        {
            list = list.Where(s => s.Month.Year == filter.Year);
        }

        if (filter.FromMonth.HasValue)
        {
            list = list.Where(s => s.Month >= filter.FromMonth.Value);
        }

        if (filter.ToMonth.HasValue)
        {
            list = list.Where(s => s.Month <= filter.ToMonth.Value);

        }
        return await list.ToListAsync();
    }
    

    public async Task<SalaryHistory?> GetSalaryByMonth(int employeeId, DateOnly month)
    {
        var salary= await _context.SalaryHistories
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == employeeId
                         && s.Month.Month == month.Month
                         && s.Month.Year == month.Year)
            .FirstOrDefaultAsync();
        
        return salary;
    }

    public async Task<bool> ExistForMonth(int employeeId, DateOnly month)
    {
        var salary= await _context.SalaryHistories
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == employeeId
                        && s.Month.Month == month.Month
                        && s.Month.Year == month.Year)
            .FirstOrDefaultAsync();
        if (salary is null)
        {
            return false;
        }
    
        return true;
    }
    

    public async Task<decimal> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateOnly month)
    {
        var startDate = new DateOnly(month.Year, month.Month, 1);
        var endDate = startDate.AddMonths(1);

        var totalPaidAmount = await _context.SalaryHistories
            .Include(s => s.Employee)
            .Where(s => s.Employee.DepartmentId == departmentId
                        && s.Month >= startDate
                        && s.Month < endDate)
            .SumAsync(s => (decimal?)s.BaseAmount + (decimal?)s.BonusAmount) ?? 0;

        return totalPaidAmount;

    }
   
    
    //TODO: later such function might be added into services if needed
    public async Task<SalaryHistory?> GetLatestSalaryHistoryAsync(int employeeId)
    {
        return await _context.SalaryHistories
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.Month) 
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<SalaryHistory?>> GetLatestSalaryHistoriesAsync()
    {
        return await _context.SalaryHistories
            .Include(s => s.Employee)
            .GroupBy(s => s.EmployeeId)
            .Select(s => s.OrderByDescending(p => p.Month).FirstOrDefault())
            .ToListAsync();
    }

    public async Task<decimal> GetDepartmentAverageSalaryAsync(int departmentId)
    {
        var salaries = await _context.SalaryHistories
            .Include(s => s.Employee)
            .Where(s => s.Employee.DepartmentId == departmentId)
            .ToListAsync();

        if (!salaries.Any())
            return 0;

        return salaries
            .GroupBy(s => s.EmployeeId)
            .Select(g => g.OrderByDescending(s => s.Month).First().ExpectedTotal)
            .Average();
    }
    


    public async Task<bool> UpdateSalaryAsync(SalaryHistory salary)
    {
       
        var currentMonth = salary.Month;
        var existing = await _context.SalaryHistories
            .FirstOrDefaultAsync(s => s.EmployeeId == salary.EmployeeId && s.Month == currentMonth);

        if (existing == null)
        {
            return false;
        }
        
        existing.BaseAmount = salary.BaseAmount;
        
        _context.SalaryHistories.Update(existing);
        var result = await _context.SaveChangesAsync();

        return result > 0;
    }
    
   
}