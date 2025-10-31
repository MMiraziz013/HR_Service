using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Filters;

public class VacationBalanceFilter
{
    public int? EmployeeId { get; set; }
    public int? Year { get; set; }
    public UserRole? UserRole { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }
}