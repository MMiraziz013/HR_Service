namespace Clean.Application.Dtos.SalaryHistory;

public class GetSalaryHistoryDto
{
    public int Id { get; set; }
    public DateOnly Month { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal ExpectedTotal { get; set; }
}