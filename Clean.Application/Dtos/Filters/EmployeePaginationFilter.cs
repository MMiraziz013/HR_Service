using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Filters;

public class EmployeePaginationFilter : PaginationFilter
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EmployeePosition? Position { get; set; }

    public string? DepartmentName { get; set; }

    public bool? IsActive { get; set; }
}