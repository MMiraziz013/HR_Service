namespace Clean.Application.Dtos.Reports.Department;

public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public string Description { get; set; } = string.Empty;
}