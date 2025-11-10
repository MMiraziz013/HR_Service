using CsvHelper.Configuration;

namespace Clean.Application.Dtos.Reports.SalaryHistory;

public sealed class SalaryDtoMap : ClassMap<SalaryHistoryDto>
{
    public SalaryDtoMap()
    {
        Map(m => m.Id).Name("Salary History ID");
        Map(m => m.EmployeeId).Name("Employee ID");
        Map(m => m.EmployeeName).Name("Employee Name");
         Map(m => m.BaseAmount).Name("Base Amount")
            .TypeConverterOption.Format("C");
        Map(m => m.BonusAmount).Name("Bonus Amount")
            .TypeConverterOption.Format("C");
        Map(m => m.Month).Name("Month").TypeConverterOption.Format("yyyy-MM-dd");
        Map(m => m.ExpectedTotal).Name("Expected Total")
            .TypeConverterOption.Format("C");
        Map(m => m.DepartmentId).Name("Department ID");
        Map(m => m.DepartmentName).Name("Department Name");
    }
}