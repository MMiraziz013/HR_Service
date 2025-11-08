namespace Clean.Application.Dtos.Department;

public class GetDepartmentPaymentsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal TotalAmount { get; set; }
}