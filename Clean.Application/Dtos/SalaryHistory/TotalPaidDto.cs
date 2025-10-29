namespace Clean.Application.Dtos.SalaryHistory;

public class TotalPaidDto
{
    public int EmployeeId { get; set; }          
    public string EmployeeName { get; set; }      
    public decimal TotalPaidAmount { get; set; }  
    public DateOnly StartDate { get; set; }      
    public DateOnly EndDate { get; set; }   
}