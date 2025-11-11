using CsvHelper.Configuration;

namespace Clean.Application.Dtos.Reports.SalaryAnomaly;

public sealed class AnomalyMapDto: ClassMap<SalaryAnomalyDto>
{
    public AnomalyMapDto()
    {
        Map(m => m.Id).Name("ID");
        Map(m => m.EmployeeId).Name("Employee ID");
        Map(m => m.EmployeeName).Name("Employee Name");
        Map(m => m.DepartmentId).Name("Department ID");
        Map(m => m.DepartmentName).Name("Department Name");

        Map(m => m.Month).Name("Month").TypeConverterOption.Format("yyyy-MM-dd");

        Map(m => m.ExpectedAmount).Name("Expected Amount").TypeConverterOption.Format("C");
        Map(m => m.ActualAmount).Name("Actual Amount").TypeConverterOption.Format("C");
        Map(m => m.DeviationPercent).Name("Deviation (%)").TypeConverterOption.Format("0.00%");
        
        Map(m => m.IsReviewed)
            .Name("Review Status")
            .Convert(row => row.Value.IsReviewed ? "Viewed" : "Not Viewed");

        Map(m => m.ReviewComment).Name("Review Comment");
    }

}