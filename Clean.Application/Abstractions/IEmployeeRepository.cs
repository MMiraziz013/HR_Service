using Clean.Application.Dtos.Filters;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Clean.Application.Abstractions;

public interface IEmployeeRepository
{
    //TODO: Finish Employee Repository methods
    Task<bool> AddAsync(Employee employee);

    Task<List<Employee>> GetEmployeesAsync(PaginationFilter filter);
    
    Task<Employee?> GetEmployeeByIdAsync(int id);
    
    Task<Employee?> GetEmployeeByFirstNameAsync(string firstname);
}