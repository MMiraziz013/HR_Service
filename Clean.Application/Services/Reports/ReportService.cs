
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports;
using Clean.Application.Dtos.Reports.Department;
using Clean.Application.Dtos.Reports.Employee;
using Clean.Application.Dtos.Reports.Payroll;
using Clean.Application.Dtos.Reports.ReportFilters;
using Clean.Application.Dtos.Reports.SalaryAnomaly;
using Clean.Application.Dtos.Reports.SalaryHistory;

namespace Clean.Application.Services.Reports;

using System.Text;
using System.Text.Json;
using CsvHelper;
using System.Globalization;
using Abstractions;

public class ReportsService : IReportsService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ISalaryHistoryRepository _salaryHistoryRepository;
    private readonly IPayrollRecordRepository _payrollRepository;
    private readonly ISalaryAnomalyRepository _salaryAnomalyRepository;
    public ReportsService(IEmployeeRepository employeeRepository, IDepartmentRepository departmentRepository,
    ISalaryHistoryRepository salaryHistoryRepository,IPayrollRecordRepository payrollRepository,ISalaryAnomalyRepository salaryAnomalyRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _salaryHistoryRepository = salaryHistoryRepository;
        _payrollRepository = payrollRepository;
        _salaryAnomalyRepository = salaryAnomalyRepository;
    }
    
    public async Task<ReportResult> GenerateEmployeeReportAsync(EmployeeReportFilter filter)
    {
        var format = (filter.Format ?? "json").ToLowerInvariant();
        var employees = await _employeeRepository.GetForReportAsync(filter.HiredAfter, filter.HiredBefore, filter.DepartmentId);

        return format switch
        {
            "json" => GenerateJsonReport(employees, "employees"),
            "csv" => GenerateCsvReport(employees, "employees"),
        
            // "excel" => GenerateExcelCompatibilityReport(employees, "employees"),
        
            _ => throw new NotSupportedException($"Report format '{format}' is not supported."),
        };
    }

    public async Task<ReportResult> GeneratePayrollReportAsync(PayrollReportFilter filter)
    {
        var format = (filter.Format ?? "json").ToLowerInvariant();
        var payrolls = await _payrollRepository.GetForReportAsync(filter.EmployeeId, filter.StartPeriod,
            filter.EndPeriod, filter.DepartmentId);

        return format switch
        {
            "json" => GenerateJsonReport(payrolls, "payrolls"),
            "csv" => GenerateCsvReport(payrolls, "payrolls"),
            
            _ => throw new NotSupportedException($"Report format '{format} is not supported.")
        };

    }
    public async Task<ReportResult> GenerateSalaryAnomalyReportAsync(SalaryAnomalyFilter filter)
    {
        var format = (filter.Format ?? "json").ToLowerInvariant();
        var anomalies = await _salaryAnomalyRepository.GetForReportAsync(filter.EmployeeId,filter.DepartmentId,filter.FromMonth,filter.ToMonth,filter.IsReviewed);

        return format switch
        {
            "json" => GenerateJsonReport(anomalies, "salary anomalies"),
            "csv" => GenerateCsvReport(anomalies, "salary anomalies"),
            
            _ => throw new NotSupportedException($"Report format '{format} is not supported.")
        };

    }
    
    public async Task<ReportResult> GenerateSalaryHistoryReportAsync(SalaryFilter filter)
    {
        var format = (filter.Format ?? "json").ToLowerInvariant();
        var salaries = await _salaryHistoryRepository.GetForReportAsync(filter.EmployeeId,filter.DepartmentId,filter.FromMonth,filter.ToMonth);

        return format switch
        {
            "json" => GenerateJsonReport(salaries, "salaries"),
            "csv" => GenerateCsvReport(salaries, "salaries"),
            
            _ => throw new NotSupportedException($"Report format '{format} is not supported.")
        };

    }
    public async Task<ReportResult> GenerateDepartmentReportAsync(DepartmentReportFilter filter)
    {
        var format = (filter.Format ?? "json").ToLowerInvariant();
        var departments = await _departmentRepository.GetDepartmentReportAsync(filter.Name, filter.MinEmployeeCount);

        return format switch
        {
            "json" => GenerateJsonReport(departments, "departments"),
            "csv" => GenerateCsvReport(departments, "departments"),

            _ => throw new NotSupportedException($"Report format '{format}' is not supported.")
        };
    }


    // private ReportResult GenerateExcelCompatibilityReport<T>(IEnumerable<T> data, string baseFileName)
    // {
    //     // Reuse the CSV generation logic
    //     var csvBytes = GenerateCsv(data);
    //     var fileName = $"{baseFileName}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"; // Use .xlsx extension
    //
    //     // Use the MIME type for Excel files. 
    //     // Excel will open this CSV data and format it.
    //     return new ReportResult(csvBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName, csvBytes.Length);
    //
    //     /* * Alternative for older Excel:
    //      * return new ReportResult(csvBytes, "application/vnd.ms-excel", fileName, csvBytes.Length);
    //      */
    // }

    
    
    
    
    private static ReportResult GenerateJsonReport<T>(IEnumerable<T> data, string baseFileName)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            data, 
            new JsonSerializerOptions { WriteIndented = false }
        );
        var fileName = $"{baseFileName}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
    
        // Using nameof for ContentType is good practice for common MIME types if they were constants
        return new ReportResult(bytes, "application/json", fileName, bytes.Length);
    }

    private ReportResult GenerateCsvReport<T>(IEnumerable<T> data, string baseFileName)
    {
        var csvBytes = GenerateCsv(data);
        var fileName = $"{baseFileName}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
    
        return new ReportResult(csvBytes, "text/csv", fileName, csvBytes.Length);
    }

    // Your existing GenerateCsv method remains the same
    private byte[] GenerateCsv<T>(IEnumerable<T> data)
    {
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true); 
    
        // Create CsvConfiguration
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.GetCultureInfo("en-US"));
    
        // IMPORTANT: To specifically enforce **USD $** formatting, use the "en-US" culture.
        // The "C" (currency) format string will now use the US Dollar symbol.

        using var csv = new CsvWriter(sw, config);

        // Register the custom map for EmployeeDto (only if T is EmployeeDto)
        if (typeof(T) == typeof(EmployeeDto))
        {
            csv.Context.RegisterClassMap<EmployeeDtoMap>();
        }
        
        else if (typeof(T) == typeof(PayrollReportDto))
        {
            csv.Context.RegisterClassMap<PayrollReportDtoMap>();
        }
        else if (typeof(T) == typeof(SalaryHistoryDto))
        {
            csv.Context.RegisterClassMap<SalaryDtoMap>(); 
        }
        else if (typeof(T) == typeof(SalaryAnomalyDto))
        {
            csv.Context.RegisterClassMap<AnomalyMapDto>(); 

        if (typeof(T) == typeof(DepartmentDto))
        {
            csv.Context.RegisterClassMap<DepartmentDtoMap>();
        }
    
        csv.WriteRecords(data);
        sw.Flush(); 
        ms.Position = 0; 
    
        return ms.ToArray();
    }
    
    
}