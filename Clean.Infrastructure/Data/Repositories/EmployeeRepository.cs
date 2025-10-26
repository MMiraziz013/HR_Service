using Clean.Application.Abstractions;
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
    
    public Task<List<Employee>> GetEmployeesAsync(PaginationFilter filter)
    {
        throw new NotImplementedException();
    }
    
    public Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        throw new NotImplementedException();
    }
    
    public Task<Employee?> GetEmployeeByFirstNameAsync(string firstname)
    {
        throw new NotImplementedException();
    }
}