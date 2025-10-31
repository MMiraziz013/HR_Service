using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public EmployeePosition Position { get; set; }
    public DateOnly HireDate { get; set; }
    
    // Base salary was present at first, but then removed because we already have it in Salary History
    // public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; }

    // Relationships
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;
    
    public List<VacationBalance> VacationBalances { get; set; } = new();
    
    public List<SalaryHistory> SalaryHistories { get; set; } = new();
    public List<VacationRecord> VacationRecords { get; set; } = new();
    
    public List<SalaryAnomaly> SalaryAnomalies { get; set; } = new();

    public List<PayrollRecord> PayrollRecords { get; set; } = new();
}
