namespace Clean.Application.Dtos.Reports.Payroll;

using CsvHelper.Configuration;

public sealed class PayrollReportDtoMap : ClassMap<PayrollReportDto>
{
    public PayrollReportDtoMap()
    {
        Map(m => m.Id).Name("Payroll ID");
        Map(m => m.EmployeeId).Name("Employee ID");
        Map(m => m.EmployeeName).Name("Employee Name");
        Map(m => m.PeriodStart).Name("Period Start").TypeConverterOption.Format("yyyy-MM-dd");
        Map(m => m.PeriodEnd).Name("Period End").TypeConverterOption.Format("yyyy-MM-dd");
        Map(m => m.GrossPay).Name("Gross Pay")
            .TypeConverterOption.Format("C"); 
        
        Map(m => m.Deductions)
            .TypeConverterOption.Format("C"); 
        
        Map(m => m.NetPay).Name("Net Pay")
            .TypeConverterOption.Format("C"); 
        
        Map(m => m.CreatedAt).Name("Created At").TypeConverterOption.Format("yyyy-MM-dd");
    }
}
