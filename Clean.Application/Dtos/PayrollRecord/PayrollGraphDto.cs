namespace Clean.Application.Dtos.PayrollRecord;

public class PayrollGraphDto
{
    public string Month { get; set; }  
    public decimal TotalNetPay { get; set; }
    public decimal TotalGrossPay { get; set; }
}