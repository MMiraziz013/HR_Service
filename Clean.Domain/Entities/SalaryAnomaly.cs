namespace Clean.Domain.Entities;

public class SalaryAnomaly
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public float DeviationPercent { get; set; }
    public bool IsReviewed { get; set; }
    public string? ReviewComment { get; set; } // for HR notes

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
}