using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
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
  
    // public async Task<bool> DeleteAsync(int id)
    // {
    //     var toDelete = await _context.SalaryHistories.FindAsync(id);
    //     if (toDelete is null)
    //     {
    //         throw new ArgumentException($"No salary with id {id} was found in database.");
    //     }
    //
    //     _context.SalaryHistories.Remove(toDelete);
    //     var isDeleted = await _context.SaveChangesAsync();
    //     return isDeleted > 0;
    // }

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

    // public async Task<string> GetEmployeeNameAsync(int employeeId)
    // {
    //     var employee = await _context.Employees
    //         .Where(e => e.Id == employeeId)
    //         .Select(e => e.FirstName)
    //         .FirstOrDefaultAsync();
    //
    //     return employee ?? "Not found";
    // }

}