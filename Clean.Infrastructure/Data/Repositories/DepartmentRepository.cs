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

    public async Task<List<Department>> GetDepartmentsAsync()
    {
        var departments = await _context.Departments.ToListAsync();
        return departments;
    }

    public async Task<Department?> GetDepartmentByIdAsync(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);
        return department;
    }

    public Task<bool> UpdateDepartmentAsync(Department department)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteDepartmentAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<Department?> GetDepartmentByNameAsync(string name)
    {
        var department = await _context.Departments
            .Where(d => d.Name.ToLower() == name.ToLower())
            .FirstOrDefaultAsync();
        return department!;
    }
}