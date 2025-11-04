namespace Clean.Application.Dtos.Filters;

public class SalaryHistoryFilter
{
    public int? EmployeeId { get; set; }             
    public DateOnly? FromMonth { get; set; }         
    public DateOnly? ToMonth { get; set; }           
    public int? Year { get; set; }       
}