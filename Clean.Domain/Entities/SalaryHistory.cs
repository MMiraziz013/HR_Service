namespace Clean.Domain.Entities;

public class SalaryHistory
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal ExpectedTotal => BaseAmount + BonusAmount;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
}