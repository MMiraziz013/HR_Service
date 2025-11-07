using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly DataContext _context;

    public EmployeeRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(Employee employee)
    {
        await _context.AddAsync(employee);
        var isAdded = await _context.SaveChangesAsync();
        return isAdded > 0;
    }

    public async Task<(List<GetEmployeeDto> Employees, int TotalRecords)> GetActiveEmployeesPaginatedAsync(EmployeePaginationFilter filter)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.User)
            .Include(e=> e.SalaryHistories)
            .AsQueryable();

        // Filter by IsActive (true = active, false = inactive, null = all)
        if (filter.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == filter.IsActive.Value);
        }

        // Case-insensitive substring matching with PostgreSQL ILIKE
        if (!string.IsNullOrEmpty(filter.FirstName))
        {
            query = query.Where(r => EF.Functions.ILike(r.FirstName!, $"%{filter.FirstName}%"));
        }

        if (!string.IsNullOrEmpty(filter.LastName))
        {
            query = query.Where(r => EF.Functions.ILike(r.LastName!, $"%{filter.LastName}%"));
        }

        if (!string.IsNullOrEmpty(filter.Email))
        {
            query = query.Where(r => EF.Functions.ILike(r.User.Email!, $"%{filter.Email}%"));

        }
        
        if (!string.IsNullOrEmpty(filter.DepartmentName))
        {
            query = query.Where(r => EF.Functions.ILike(r.Department.Name!, $"%{filter.DepartmentName}%"));
        }

        if (filter.Position.HasValue)
        {
            query = query.Where(r => r.Position == filter.Position);
        }
        
        var totalRecords = await query.CountAsync();

        query = query
            .OrderBy(e => e.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);

        var employees = await query.Select(e => new GetEmployeeDto
        {
            Id = e.Id,
            FirstName = e.FirstName,
            LastName = e.LastName,
            DepartmentName = e.Department.Name,
            BaseSalary = e.SalaryHistories
                .OrderByDescending(sh => sh.Month)
                .Select(sh => sh.BaseAmount)
                .FirstOrDefault(),
            HireDate = e.HireDate.ToString("yyyy-MM-dd"),
            Position = e.Position,
            IsActive = e.IsActive
        }).ToListAsync();

        return (employees, totalRecords);
    }

    public async Task<List<Employee>> GetActiveEmployeesAsync()
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.User)
            .Include(e => e.SalaryHistories)
            .Include(e=> e.VacationBalances)
            .Where(e => e.IsActive)
            .ToListAsync();

        return employees;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e=> e.SalaryHistories)
            .Include(e=> e.VacationBalances)
            .FirstOrDefaultAsync(e => e.Id == id);
        return employee;
    }

    public async Task<Employee?> GetEmployeeByUserId(int userId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e=> e.SalaryHistories)
            .Include(e=> e.VacationBalances)
            .FirstOrDefaultAsync(e => e.UserId == userId);
        return employee;
    }

    public async Task<Employee?> UpdateEmployeeAsync(Employee employee)
    {
        var existing = await _context.Employees
            .Include(e => e.Department)
            .Include(e=> e.SalaryHistories)
            .FirstOrDefaultAsync(e => e.Id == employee.Id);

        if (existing == null)
            return null;

        _context.Entry(existing).CurrentValues.SetValues(employee);
        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<List<Employee>> GetActiveEmployeesByDepartmentAsync(int departmentId)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive && e.DepartmentId == departmentId)
            .ToListAsync();

        return employees;
    }


    public async Task<bool> DeactivateEmployeeAsync(int id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return false;

        employee.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
