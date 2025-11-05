namespace Clean.Application.Dtos.SalaryHistory;

public class GetSalaryHistoryWithEmployeeDto
{
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal ExpectedTotal { get; set; }
        public decimal Bonus { get; set; } 
        public decimal Base { get; set; }
        public DateOnly Month { get; set; }
}