using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Filters;

public class VacationRecordPaginationFilter : PaginationFilter
{
    public int? Id { get; set; }
    public int? EmployeeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public VacationStatus? VacationStatus { get; set; }
}