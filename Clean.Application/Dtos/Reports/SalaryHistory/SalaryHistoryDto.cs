namespace Clean.Application.Dtos.Reports.SalaryHistory;

public class SalaryHistoryDto
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal ExpectedTotal { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;


}