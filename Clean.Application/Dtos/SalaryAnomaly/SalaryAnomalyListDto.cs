namespace Clean.Application.Dtos.PayrollRecord;

public class SalaryAnomalyListDto
{
    public string FullName { get; set; }
    public DateOnly Month { get; set; }
    public float Deviation { get; set; }
    public bool IsViewed { get; set; }
}