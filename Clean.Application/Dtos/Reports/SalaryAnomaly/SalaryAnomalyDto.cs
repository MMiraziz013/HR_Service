namespace Clean.Application.Dtos.Reports.SalaryAnomaly;

public class SalaryAnomalyDto
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public float DeviationPercent { get; set; }
    public bool IsReviewed { get; set; }
    public string ReviewComment { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

}