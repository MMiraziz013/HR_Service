using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Employee;

public class UpdateEmployeeDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; } = default!;
    public string? LastName { get; set; } = default!;
    public EmployeePosition? Position { get; set; }
    public DateOnly? HireDate { get; set; }
    public decimal? BaseSalary { get; set; }
    public bool? IsActive { get; set; }
    
    public int? DepartmentId { get; set; }
}