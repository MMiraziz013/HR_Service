using Clean.Application.Dtos.Department;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IDepartmentService
{
    public Task<Response<bool>> AddDepartmentAsync(AddDepartmentDto dto);

    public Task<Response<List<GetDepartmentDto>>> GetDepartmentsAsync();

    Task<Response<List<GetDepartmentWithEmployeesDto>>> GetDepartmentsWithEmployeesAsync();

    //TODO: Finish the Department Methods
}