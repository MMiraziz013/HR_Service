using Clean.Domain.Enums;

namespace Clean.Application.Dtos.VacationRecords;

public class AddVacationRecordDto
{
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType Type { get; set; }  // Paid or Unpaid
    public VacationStatus Status { get; set; } // Pending, Approved, Rejected
    
    //TODO: Check late if we need more details vacation type (subtype)
    //public VacationSubtype Subtype { get; set; } // e.g. Regular, Maternity, Sick, Unpaid

    public decimal? PaymentAmount { get; set; }
    public string? ManagerComment { get; set; }  // Optional HR feedback
}