namespace Clean.Application.Dtos.PayrollRecord;

public class AddPayrollRecordDto
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal Deductions { get; set; }
    public int EmployeeId { get; set; }

}