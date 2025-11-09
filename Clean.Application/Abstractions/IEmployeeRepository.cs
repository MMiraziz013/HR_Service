using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports;
using Clean.Application.Dtos.Reports.Employee;
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

    
    /// <summary>
    /// Retrieves a list of employee DTOs specifically tailored for reporting purposes.
    /// </summary>
    /// <param name="hiredAfter">Filter for employees hired on or after this date.</param>
    /// <param name="hiredBefore">Filter for employees hired on or before this date.</param>
    /// <param name="departmentId">Filter for department from which employee should be.</param>
    /// <returns>A task that returns an IEnumerable of EmployeeDto.</returns>
    Task<IEnumerable<EmployeeDto>> GetForReportAsync(DateTime? hiredAfter, DateTime? hiredBefore, int? departmentId);
    
}