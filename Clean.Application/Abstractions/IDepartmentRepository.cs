using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<bool> AddDepartmentAsync(Department department);
    Task<List<Department>> GetDepartmentsAsync(string? search = null);
    Task<Department?> GetDepartmentByIdAsync(int id);
    Task<bool> UpdateDepartmentAsync(Department department);
    Task<bool> DeleteDepartmentAsync(int id);

    Task<Department?> GetDepartmentByNameAsync(string name);
    
    //TODO: Finish Department repo methods
}