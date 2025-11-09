using CsvHelper.Configuration;

namespace Clean.Application.Dtos.Reports.Employee;

public sealed class EmployeeDtoMap : ClassMap<EmployeeDto>
{
    public EmployeeDtoMap()
    {
        Map(m => m.Id).Name("Employee ID");
        Map(m => m.FullName).Name("Full Name");
        Map(m => m.Email).Name("Email Address");
        Map(m => m.HireDate).Name("Hire Date");
        Map(m => m.Role).Name("Role");
        Map(m => m.Department).Name("Department Name");

        Map(m => m.CurrentSalary)
            .TypeConverterOption.Format("C"); 

        Map(m => m.Position);
    }
}