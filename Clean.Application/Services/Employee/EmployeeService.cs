using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.Employee;

public class EmployeeService : IEmployeeService
{


    public Task<List<Domain.Entities.Employee>> GetEmployeesAsync(PaginationFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<Domain.Entities.Employee?> GetEmployeeByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Domain.Entities.Employee?> GetEmployeeByFirstNameAsync(string firstname)
    {
        throw new NotImplementedException();
    }
}