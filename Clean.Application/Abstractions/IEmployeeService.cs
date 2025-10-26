using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IEmployeeService
{
    //TODO: Finish Employee Service methods
    
    Task<List<Employee>> GetEmployeesAsync(PaginationFilter filter);
    
    Task<Employee?> GetEmployeeByIdAsync(int id);
    
    Task<Employee?> GetEmployeeByFirstNameAsync(string firstname);
}