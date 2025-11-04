namespace Clean.Application.Dtos.SalaryAnomaly;

public class GetSalaryAnomalyDto
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public float DeviationPercent { get; set; }
    public bool IsViewed { get; set; }
    public string? ReviewComment { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; }

}