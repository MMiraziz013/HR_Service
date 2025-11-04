using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.VacationBalance;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
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

    public async Task<bool> ExistsAsync(int employeeId, int year)
    {
        return await _context.VacationBalances
            .AnyAsync(v => v.EmployeeId == employeeId && v.Year == year);

    }

    //TODO: Check later if we need delete method for VacationBalance
}