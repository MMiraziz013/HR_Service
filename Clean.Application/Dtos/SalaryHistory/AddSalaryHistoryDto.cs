namespace Clean.Application.Dtos.SalaryHistory;

public class AddSalaryHistoryDto
{
    public int EmployeeId { get; set; }
    public DateOnly Month { get; set; }
    public decimal BaseAmount { get; set; }
}