using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Reports.VacationBalance;

public class VacationBalanceDto
{
    public int Id { get; set; }

    // Employee Info
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    
    public int WorkedYears { get; set; }
    public string Position { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;


    // Vacation Statistics
    public int ByExperienceBonusDays { get; set; }
    public int TotalDaysPerYear { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDaysPerYear - UsedDays;
    public int VacationsTaken { get; set; }
    public int DaysPerYear => TotalDaysPerYear - ByExperienceBonusDays;

    // Flags
    public bool HasBonusDays => ByExperienceBonusDays > 0;
    public bool HasUsedDays => UsedDays > 0;
    public bool IsLimitFinished => RemainingDays <= 0;

    // Period Info
    public int Year { get; set; }
    public DateOnly BalanceFrom { get; set; }
    public DateOnly BalanceTo { get; set; }
}
