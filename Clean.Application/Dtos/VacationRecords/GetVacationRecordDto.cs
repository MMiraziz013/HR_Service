using Clean.Application.Dtos.Employee;
using Clean.Domain.Enums;

namespace Clean.Application.Dtos.VacationRecords;

public class GetVacationRecordDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public GetEmployeeDto Employee { get; set; } = null!;

    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public VacationType Type { get; set; }  // Paid or Unpaid
    public VacationStatus Status { get; set; } // Pending, Approved, Rejected
    
    public int DaysCount { get; set; }
    public decimal? PaymentAmount { get; set; }
    // public string? ManagerComment { get; set; }  // Optional HR feedback
}