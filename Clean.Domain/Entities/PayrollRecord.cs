namespace Clean.Domain.Entities;

public class PayrollRecord
{
    public int Id { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal GrossPay { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay => GrossPay - Deductions;
    public DateTime CreatedAt { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
}