using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Clean.Application.Abstractions;

public interface IEmployeeRepository
{
    //TODO: Finish Employee Repository methods
    Task<bool> AddAsync(Employee employee);

    Task<(List<GetEmployeeDto> Employees, int TotalRecords)> GetActiveEmployeesPaginatedAsync(EmployeePaginationFilter filter);

    Task<List<Employee>> GetActiveEmployeesAsync();
    
    Task<Employee?> GetEmployeeByIdAsync(int id);

    Task<Employee?> UpdateEmployeeAsync(Employee employee);

    Task<bool> DeactivateEmployeeAsync(int id);
}