using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class PayrollRecordRepository: IPayrollRecordRepository
{
    private readonly DataContext _context;

    public PayrollRecordRepository(DataContext context)
    {
        _context = context;
    }
    
    public async Task<bool> AddAsync(PayrollRecord payroll)
    {
        bool exists = await _context.PayrollRecords
            .AnyAsync(p => p.EmployeeId == payroll.EmployeeId &&
                           (
                               (payroll.PeriodStart >= p.PeriodStart && payroll.PeriodStart <= p.PeriodEnd) ||
                               (payroll.PeriodEnd >= p.PeriodStart && payroll.PeriodEnd <= p.PeriodEnd) ||     
                               (payroll.PeriodStart <= p.PeriodStart && payroll.PeriodEnd >= p.PeriodEnd)      
                           ));
        if (exists)
            return false;

        await _context.PayrollRecords.AddAsync(payroll);
        await _context.SaveChangesAsync();
        return true;
    }



    public async Task<PayrollRecord?> GetByIdAsync(int id)
    {
        return await _context.PayrollRecords
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<PayrollRecord>> GetAllAsync()
    {
        return await _context.PayrollRecords
            .Include(p => p.Employee)
            .OrderByDescending(p => p.PeriodStart).ToListAsync();
    }

    public async Task<List<PayrollRecord>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.PayrollRecords
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(s => s.PeriodStart)
            .ToListAsync();
    }

    public  async Task<PayrollRecord?> GetLatestByEmployeeIdAsync(int employeeId)
    {
        return await _context.PayrollRecords
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PeriodStart)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(PayrollRecord payroll)
    {
        var entity = await _context.PayrollRecords.FindAsync(payroll.Id);
        if (entity == null)
        {
            return false;
        }

        entity.Deductions = payroll.Deductions;
        
        _context.PayrollRecords.Update(entity);
        var isUpdated = await _context.SaveChangesAsync();
        return isUpdated > 0;

    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.PayrollRecords.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _context.PayrollRecords.Remove(entity);
        var isDeleted = await _context.SaveChangesAsync();
        return isDeleted > 0;
    }
}