using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Reports.VacationRecord;

public class VacationRecordDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int VacationDays { get; set; }  
    public string VacationType { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public string Status { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}