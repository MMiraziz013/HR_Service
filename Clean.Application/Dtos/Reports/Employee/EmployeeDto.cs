namespace Clean.Application.Dtos.Reports.Employee;

public class EmployeeDto
{
    // The property names become the CSV column headers (e.g., "Id", "FullName")
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal CurrentSalary { get; set; }
}