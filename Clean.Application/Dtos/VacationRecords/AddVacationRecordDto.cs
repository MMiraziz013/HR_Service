using Clean.Domain.Enums;

namespace Clean.Application.Dtos.VacationRecords;

public class AddVacationRecordDto
{
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType Type { get; set; }  // Paid or Unpaid
    public decimal PaymentAmount { get; set; }
}