namespace Clean.Application.Dtos.VacationBalance;

public class GetVacationBalanceDto
{
    public int Id { get; set; }
    public int TotalDaysPerYear { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDaysPerYear - UsedDays;
    public int Year { get; set; }
    
    public int EmployeeId { get; set; }
    public Domain.Entities.Employee Employee { get; set; } = default!;
    
    //TODO: Check the the dto properties, if they are correct
}