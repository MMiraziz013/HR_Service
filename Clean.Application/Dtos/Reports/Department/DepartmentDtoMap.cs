using CsvHelper.Configuration;

namespace Clean.Application.Dtos.Reports.Department;

public sealed class DepartmentDtoMap : ClassMap<DepartmentDto>
{
    public DepartmentDtoMap()
    {
        Map(d => d.DepartmentId).Name("Department ID");
        Map(d => d.Name).Name("Department Name");
        Map(d => d.EmployeeCount).Name("Employee Count");
        Map(d => d.Description).Name("Department Description");
    }
}