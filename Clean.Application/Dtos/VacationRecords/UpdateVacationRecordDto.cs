using Clean.Application.Dtos.Employee;
using Clean.Domain.Enums;

namespace Clean.Application.Dtos.VacationRecords;

public class UpdateVacationRecordDto
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public VacationType? Type { get; set; }  // Paid or Unpaid
    public VacationStatus? Status { get; set; } // Pending, Approved, Rejected
    public decimal? PaymentAmount { get; set; }
    // public string? ManagerComment { get; set; }  // Optional HR feedback
}