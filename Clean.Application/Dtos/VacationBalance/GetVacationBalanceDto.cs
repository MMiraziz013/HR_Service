using Clean.Application.Dtos.Employee;

namespace Clean.Application.Dtos.VacationBalance;

public class GetVacationBalanceDto
{
    public int Id { get; set; }
    public int TotalDaysPerYear { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDaysPerYear - UsedDays;
    public int Year { get; set; }

    public int ByExperienceBonusDays { get; set; }
    
    public string PeriodStart { get; set; }
    public string PeriodEnd { get; set; }
    public int EmployeeId { get; set; }

    public GetEmployeeDto Employee { get; set; } = null!;
}