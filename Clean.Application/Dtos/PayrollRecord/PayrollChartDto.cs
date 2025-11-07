namespace Clean.Application.Dtos.PayrollRecord;

public class PayrollChartDto
{
    public string Month { get; set; } = default!;
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
}
