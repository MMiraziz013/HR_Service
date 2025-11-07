using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Clean.Application.Abstractions;

public interface IEmployeeRepository
{
    Task<bool> AddAsync(Employee employee);

    Task<(List<GetEmployeeDto> Employees, int TotalRecords)> GetActiveEmployeesPaginatedAsync(EmployeePaginationFilter filter);

    Task<List<Employee>> GetActiveEmployeesAsync();
    
    Task<Employee?> GetEmployeeByIdAsync(int id);

    Task<Employee?> GetEmployeeByUserId(int userId);

    Task<Employee?> UpdateEmployeeAsync(Employee employee);

    Task<bool> DeactivateEmployeeAsync(int id);
    Task<List<Employee>> GetActiveEmployeesByDepartmentAsync(int departmentId);
}