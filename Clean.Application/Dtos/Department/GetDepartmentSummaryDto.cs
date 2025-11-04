namespace Clean.Application.Dtos.Department;

public class GetDepartmentSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int EmployeeCount { get; set; }
}
