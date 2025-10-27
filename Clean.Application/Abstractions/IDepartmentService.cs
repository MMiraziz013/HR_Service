using Clean.Application.Dtos.Department;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IDepartmentService
{
    public Task<Response<bool>> AddDepartmentAsync(AddDepartmentDto dto);

    Task<Response<List<GetDepartmentDto>>> GetDepartmentsAsync(string? search = null);

    Task<Response<List<GetDepartmentWithEmployeesDto>>> GetDepartmentsWithEmployeesAsync(string? search = null);

    Task<Response<GetDepartmentDto?>> GetDepartmentByIdAsync(int id);

    Task<Response<GetDepartmentWithEmployeesDto?>> GetDepartmentByIdWithEmployeesAsync(int id);

    Task<Response<GetDepartmentDto>> UpdateDepartmentAsync(UpdateDepartmentDto dto);

    Task<Response<bool>> DeleteDepartmentAsync(int id);
}