namespace Clean.Application.Dtos.VacationBalance;

public class UpdateVacationBalanceDto
{
    public int Id { get; set; }
    public int? TotalDaysPerYear { get; set; }
    public int? UsedDays { get; set; }
    public int? Year { get; set; }
    
    public int? ByExperienceBonusDays { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
}