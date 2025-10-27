namespace Clean.Application.Dtos.SalaryHistory;

public class GetTotalPaidForDepartmentDto
{
  public int DepartmentId { get; set; }
  public string DepartmentName { get; set; }
    public decimal TotalPaidAmount { get; set; }  
    public DateOnly StartDate { get; set; }      
    public DateOnly EndDate { get; set; }   
}