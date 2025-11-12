using Clean.Application.Abstractions;
using Clean.Application.Dtos.Reports.SalaryAnomaly;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class SalaryAnomalyRepository : ISalaryAnomalyRepository
{
    private readonly DataContext _context;

    public SalaryAnomalyRepository(DataContext context)
    {
        _context = context;
    }
    public async Task<bool> AddAsync(SalaryAnomaly anomaly)
    {
        await _context.SalaryAnomalies.AddAsync(anomaly);
        var isAdded = await _context.SaveChangesAsync();
        return isAdded > 0;
    }

    public Task<List<SalaryAnomaly>> GetAllAsync()
    {
        var query = _context.SalaryAnomalies
            .Include(s => s.Employee)
            .AsQueryable();
        return query.ToListAsync();
    }

    public async Task<IEnumerable<SalaryAnomalyDto>> GetForReportAsync(int? employeeId, int? departmentId,
        DateOnly? fromMonth, DateOnly? toMonth,bool? isViewed)
    {
        IQueryable<SalaryAnomaly> query =  _context.SalaryAnomalies
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

        if (isViewed.HasValue)
        {
            query = query.Where(e => e.IsReviewed == isViewed);
        }

        var anomalies = await query
            .Select(e => new SalaryAnomalyDto
            {
                Id = e.Id,
                Month = e.Month,
                ActualAmount = e.ActualAmount,
                DepartmentId = e.Employee.DepartmentId,
                DepartmentName = e.Employee.Department.Name,
                DeviationPercent = e.DeviationPercent,
                EmployeeId = e.EmployeeId,
                EmployeeName = $"{e.Employee.FirstName} {e.Employee.LastName}",
                ExpectedAmount = e.ExpectedAmount,
                IsReviewed = e.IsReviewed,
                ReviewComment = e.ReviewComment ?? ""
            }).ToListAsync();

        return anomalies;
    }
    public async Task<List<SalaryAnomaly>> GetByEmployeeIdAsync(int employeeId)
    {
        var anomalies= _context.SalaryAnomalies
            .Include(e => e.Employee)
            .Where(s => s.EmployeeId == employeeId).AsQueryable();
        return await anomalies.ToListAsync();
    }

    public async Task<SalaryAnomaly?> GetByIdAsync(int id)
    {
        var entity = await _context.SalaryAnomalies
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);
        return entity;
    }

    public async Task<List<SalaryAnomaly>> GetUnviewedAsync()
    {
        var anomalies = await _context.SalaryAnomalies
            .Include(s => s.Employee)
            .Where(s => s.IsReviewed == false)
            .ToListAsync();
        return anomalies;
    }

    public async Task<bool> MarkAsViewedAsync(int id)
    {
            var anomaly = await _context.SalaryAnomalies.FindAsync(id);
            if (anomaly == null)
                return false; 
            anomaly.IsReviewed = true;
            await _context.SaveChangesAsync();
            return true;
    }

    public async Task<bool> ExistsForEmployeeAndMonthAsync(int employeeId, DateOnly month)
    {
        return await _context.SalaryAnomalies
            .AnyAsync(s => s.EmployeeId == employeeId && s.Month.Month == month.Month && s.Month.Year == month.Year);
    }

    public async Task<bool> UpdateAsync(SalaryAnomaly salary)
    {
        var existing = await _context.SalaryAnomalies.FindAsync(salary.Id);
        if (existing is null)
        {
            return false;
        }
        
        existing.IsReviewed = salary.IsReviewed;
        existing.ReviewComment = salary.ReviewComment;
        existing.Month = salary.Month;
        existing.ExpectedAmount = salary.ExpectedAmount;
        existing.ActualAmount = salary.ActualAmount;
        existing.ReviewComment = salary.ReviewComment;
        _context.SalaryAnomalies.Update(existing);
        var result = await _context.SaveChangesAsync();

        return result > 0;
    }

    public async Task<bool> DeleteAnomalyAsync(int id)
    {
        var existing = await _context.SalaryAnomalies.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        _context.SalaryAnomalies.Remove(existing);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<List<SalaryAnomaly>> GetSalaryAnomalyByEmployeeId(int id)
    {
        var anomalies = await _context.SalaryAnomalies
            .Include(e => e.Employee)
            .Where(e => e.EmployeeId == id).ToListAsync();
        return anomalies;
    }
}