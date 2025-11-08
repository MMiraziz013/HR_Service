namespace Clean.Application.Dtos.SalaryHistory;

public class UpdateSalaryDto
{
    public int EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal? Bonus { get; set; }
}