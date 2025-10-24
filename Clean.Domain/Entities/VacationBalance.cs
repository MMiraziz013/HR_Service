namespace Clean.Domain.Entities;

public class VacationBalance
{
    public int Id { get; set; }
    public int TotalDaysPerYear { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDaysPerYear - UsedDays;
    public int Year { get; set; }
    
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
}