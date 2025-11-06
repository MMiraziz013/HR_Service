namespace Clean.Application.Dtos.VacationRecords;

public class VacationSummaryDto
{
    public string Month { get; set; } = default!; // Format: "YYYY-MM"
    public int TotalVacationDays { get; set; }
    public int EmployeesOnVacation { get; set; }
}
