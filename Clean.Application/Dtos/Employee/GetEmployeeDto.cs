using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Employee;

public class GetEmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public EmployeePosition Position { get; set; }
    public string? HireDate { get; set; }
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; }

    public string DepartmentName { get; set; } = default!;
    
    //TODO: Check if we need below properties in the future
    
    // public VacationBalance VacationBalance { get; set; } = default!;
    //
    // public List<SalaryHistory> SalaryHistories { get; set; } = new();
    // public List<VacationRecord> VacationRecords { get; set; } = new();
    //
    // public List<SalaryAnomaly> SalaryAnomalies { get; set; } = new();
    //
    // public List<PayrollRecord> PayrollRecords { get; set; } = new();
}