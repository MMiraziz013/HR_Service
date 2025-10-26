using Clean.Application.Abstractions;
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
}