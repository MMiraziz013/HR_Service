using Clean.Application.Dtos.Employee;

namespace Clean.Application.Dtos.Department;

public class GetDepartmentWithEmployeesDto : GetDepartmentDto
{
    public List<GetEmployeeDto> Employees { get; set; }
}