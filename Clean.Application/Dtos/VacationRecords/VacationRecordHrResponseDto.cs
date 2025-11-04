using Clean.Domain.Enums;

namespace Clean.Application.Dtos.VacationRecords;

public class VacationRecordHrResponseDto
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public VacationStatus UpdatedStatus { get; set; }
}