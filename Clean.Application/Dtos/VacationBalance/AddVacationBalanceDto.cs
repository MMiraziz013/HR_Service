namespace Clean.Application.Dtos.VacationBalance;

public class AddVacationBalanceDto
{
    public int TotalDaysPerYear { get; set; }

    public int ByExperienceBonusDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDaysPerYear - UsedDays;
    public int Year { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int EmployeeId { get; set; }
    
    //TODO: Check the the dto properties, if they are correct
}