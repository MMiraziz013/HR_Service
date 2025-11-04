namespace Clean.Application.Dtos.VacationRecords;

public class VacationCheckDto
{
    public bool IsAvailable { get; set; }
    public string? Message { get; set; }
    public decimal PaymentAmount { get; set; }
}