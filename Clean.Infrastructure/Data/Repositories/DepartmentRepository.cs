using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly DataContext _context;

    public DepartmentRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<bool> AddDepartmentAsync(Department department)
    {
        var exists = await GetDepartmentByNameAsync(department.Name);
        if (exists != null)
        {
            return false;
        }

        await _context.Departments.AddAsync(department);
        var isAdded = await _context.SaveChangesAsync();
        return isAdded > 0;
    }

    public async Task<List<Department>> GetDepartmentsAsync(string? search = null)
    {
        var query = _context.Departments
            .Include(d => d.Employees)
            .ThenInclude(e=> e.SalaryHistories)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => EF.Functions.ILike(d.Name!, $"%{search}%"));
        }

        return await query.OrderBy(d => d.Id).ToListAsync();
    }

    public async Task<Department?> GetDepartmentByIdAsync(int id)
    {
        return await _context.Departments
            .Include(d => d.Employees)
            .ThenInclude(e=> e.SalaryHistories)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> UpdateDepartmentAsync(Department department)
    {
        var existing = await _context.Departments.FindAsync(department.Id);
        if (existing == null)
        {
            return false;
        }

        existing.Name = department.Name;
        existing.Description = department.Description;

        _context.Departments.Update(existing);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteDepartmentAsync(int id)
    {
        var existing = await _context.Departments.FindAsync(id);
        if (existing == null)
        {
            return false;
        }

        _context.Departments.Remove(existing);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<Department?> GetDepartmentByNameAsync(string name)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => EF.Functions.ILike(d.Name!, $"%{name}%"));
    }
}
