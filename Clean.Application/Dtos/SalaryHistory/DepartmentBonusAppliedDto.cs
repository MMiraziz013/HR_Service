namespace Clean.Application.Dtos.SalaryHistory;

public class DepartmentBonusAppliedDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal ExpectedTotal { get; set; }
}
