using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports.Payroll;
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

    public async Task<IEnumerable<PayrollReportDto>> GetForReportAsync(int? employeeId, DateOnly? startDate,
        DateOnly? endDate, int? departmentId)
    {
        IQueryable<PayrollRecord> query = _context.PayrollRecords
            .Include(e => e.Employee)
            .Include(e => e.Employee.Department)
            .OrderBy(e => e.Id);

        if (employeeId.HasValue)
        {
           query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.PeriodStart >= startDate);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.PeriodEnd <= endDate);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.Employee.DepartmentId == departmentId);
        }

        var payrolls = await query
            .Select(e => new PayrollReportDto
            {
                Id = e.Id,
                CreatedAt = e.CreatedAt,
                Deductions = e.Deductions,
                EmployeeId = e.EmployeeId,
                EmployeeName = $"{e.Employee.FirstName} {e.Employee.LastName}",
                GrossPay = e.GrossPay,
                NetPay = e.NetPay,
                PeriodStart = e.PeriodStart,
                PeriodEnd = e.PeriodEnd
            }).ToListAsync();
        return payrolls;
    }
    
    public async Task<PayrollRecord?> GetPayrollByMonthAsync(int employeeId, DateOnly month)
    {
        var startOfMonth = new DateOnly(month.Year, month.Month, 1);
        var endOfMonth = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

        var payroll = await _context.PayrollRecords
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == employeeId &&
                        p.PeriodStart >= startOfMonth &&
                        p.PeriodStart <= endOfMonth)
            .FirstOrDefaultAsync();

        return payroll;
    }

    public async Task<List<PayrollRecord>> GetPayrollRecordsByMonthAsync(int year, int month)
    {
        return await _context.PayrollRecords
            .Where(p => p.PeriodStart.Year == year && p.PeriodStart.Month == month)
            .ToListAsync();
    }


    public async Task<IEnumerable<PayrollRecord>> GetPayrollRecordsAsync(PayrollRecordFilter filter)
    {
        var query = _context.PayrollRecords
            .Include(p => p.Employee)
            .AsQueryable();

        if (filter.EmployeeId.HasValue)
            query = query.Where(p => p.EmployeeId == filter.EmployeeId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(p => p.PeriodStart >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(p => p.PeriodEnd <= filter.ToDate.Value);

        if (filter.MinNetPay.HasValue)
            query = query.Where(p => p.NetPay >= filter.MinNetPay.Value);

        if (filter.MaxNetPay.HasValue)
            query = query.Where(p => p.NetPay <= filter.MaxNetPay.Value);

        return await query.ToListAsync();
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
    public  async Task<List<PayrollRecord?>> GetLatestPayrollAsync()
    {
      return await _context.PayrollRecords
          .Include(p=>p.Employee)
          .GroupBy(s=>s.EmployeeId)
          .Select(s=>s.OrderByDescending(p=>p.PeriodEnd).FirstOrDefault())
          .ToListAsync();
    }

    public async Task<decimal> GetTotalPaidForMonth(DateOnly month)
    {
        return await _context.PayrollRecords
            .Where(p => p.PeriodStart.Month == month.Month && p.PeriodStart.Year == month.Year)
            .SumAsync(p => p.GrossPay - p.Deductions); 
    }

    public async Task<decimal> GetDepartmentExpectedAverageAsync(int departmentId)
    {
        var payrolls = await _context.PayrollRecords
            .Include(s => s.Employee)
            .Where(s => s.Employee.DepartmentId == departmentId)
            .ToListAsync();
        
        return payrolls.Any() ? payrolls.Average(p => p.GrossPay) : 0m;
    }


    public async Task<decimal> GetPositionExpectedAverageAsync(int departmentId, string position)
    {
        var normalizedPosition = position.ToLower();

        var payrolls = await _context.PayrollRecords
            .Include(s => s.Employee)
            .Where(s => s.Employee.DepartmentId == departmentId &&
                        s.Employee.Position.ToString().ToLower() == normalizedPosition)
            .ToListAsync();

        return payrolls.Any() ? payrolls.Average(p => p.GrossPay) : 0m;
    }
    
    public async Task<List<PayrollRecord>> GetPayrollRecordsAsync(DateTime startMonth, DateTime endMonth)
    {
        var startDate = new DateTime(startMonth.Year, startMonth.Month, 1);
        var endDate = new DateTime(endMonth.Year, endMonth.Month, 1).AddMonths(1).AddDays(-1);
    
        return await _context.PayrollRecords
            .Where(r => r.CreatedAt.Date >= startDate.Date && r.CreatedAt.Date <= endDate.Date)
            .ToListAsync();
    
    }
   // public async Task<decimal> GetPositionActualAverageAsync(int departmentId, string position)
   //  {
   //      var normalizedPosition = position.ToLower();
   //
   //      var payrolls = await _context.PayrollRecords
   //          .Include(s => s.Employee)
   //          .Where(s => s.Employee.DepartmentId == departmentId &&
   //                      s.Employee.Position.ToString().ToLower() == normalizedPosition)
   //          .ToListAsync();
   //
   //      return payrolls.Any() ? payrolls.Average(p => p.GrossPay) : 0m;
   //  }

    // public async Task<decimal> GetDepartmentActualAverageAsync(int departmentId)
    // {
    //     var payrolls = await _context.PayrollRecords
    //         .Include(s => s.Employee)
    //         .Where(s => s.Employee.DepartmentId == departmentId)
    //         .ToListAsync();
    //     
    //     return payrolls.Any() ? payrolls.Average(p => p.GrossPay) : 0m;
    // }
    
   

}