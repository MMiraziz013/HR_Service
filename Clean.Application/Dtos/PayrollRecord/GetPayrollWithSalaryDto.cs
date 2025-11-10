namespace Clean.Application.Dtos.PayrollRecord;

public class GetPayrollWithSalaryDto
{
    public int Id { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal GrossPay { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } 
    
    public decimal? BaseSalary { get; set; }
    public decimal? Bonus { get; set; }
}