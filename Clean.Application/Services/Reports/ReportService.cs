using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Reports;
using Clean.Application.Dtos.Reports.Department;
using Clean.Application.Dtos.Reports.Employee;
using Clean.Application.Dtos.Reports.ReportFilters;

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


    public ReportsService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ISalaryHistoryRepository salaryHistoryRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _salaryHistoryRepository = salaryHistoryRepository;
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