using Clean.Application.Abstractions;
using Clean.Application.Dtos.SalaryHistory;
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

    // public  async Task<List<SalaryHistory>> GetSalaryHistoryByEmailAsync(string email)
    // {
    //     return await _context.SalaryHistories
    //         .Where(s => s. == employeeId)
    //         .OrderByDescending(s => s.Month)
    //         .ToListAsync();
    // }

    public async Task<SalaryHistory?> GetByIdAsync(int id)
    {
        return await _context.SalaryHistories
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var toDelete = await _context.SalaryHistories.FindAsync(id);
        if (toDelete is null)
        {
            throw new ArgumentException($"No salary with id {id} was found in database.");
        }

        _context.SalaryHistories.Remove(toDelete);
        var isDeleted = await _context.SaveChangesAsync();
        return isDeleted > 0;
    }

    public async Task<SalaryHistory> GetSalaryByMonth(int employeeId, DateOnly month)
    {
        var salary= await _context.SalaryHistories
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == employeeId
                        && s.Month.Month == month.Month
                        && s.Month.Year == month.Year)
            .FirstOrDefaultAsync();
        if (salary is null)
        {
            throw new ArgumentException($"No salary for employee with Id:  {employeeId} was found in database.");
        }

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

    public async Task<decimal> GetTotalPaidAmountAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var totalPaidAmount = await _context.SalaryHistories
            .Where(s => s.EmployeeId == employeeId
                        && s.Month >= DateOnly.FromDateTime(startDate)
                        && s.Month <= DateOnly.FromDateTime(endDate))
            .SumAsync(s => s.ExpectedTotal);

        return totalPaidAmount;
    }

    public async Task<decimal> GetTotalPaidAmountByDepartmentAsync(int departmentId, DateTime startDate, DateTime endDate)
    {
        var totalPaidAmount = await _context.SalaryHistories
            .Where(s => s.Employee.DepartmentId == departmentId
                        && s.Month >= DateOnly.FromDateTime(startDate)
                        && s.Month <= DateOnly.FromDateTime(endDate))
            .SumAsync(s => s.ExpectedTotal);

        return totalPaidAmount;
    }

}