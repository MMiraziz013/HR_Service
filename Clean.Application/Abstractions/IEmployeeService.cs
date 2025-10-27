using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IEmployeeService
{
    Task<PaginatedResponse<GetEmployeeDto>> GetEmployeesAsync(EmployeePaginationFilter filter);

    Task<Response<GetEmployeeDto?>> GetEmployeeByIdAsync(int id);

    Task<Response<GetEmployeeDto>> UpdateEmployeeAsync(UpdateEmployeeDto dto);
    
    Task<Response<bool>> DeactivateEmployeeAsync(int id);
}