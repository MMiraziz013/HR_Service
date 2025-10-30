namespace Clean.Domain.Entities;

public class SalaryHistory
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal ExpectedTotal => BaseAmount + BonusAmount;
    
    //TODO: Check later if need to/can add this property
    // public int DaysWorked { get; set; } // number of payable days in that month

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
}