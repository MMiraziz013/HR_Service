using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.PayrollRecord;

public class PayrollRecordService : IPayrollRecordService
{
    private readonly IPayrollRecordRepository _payrollRecordRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ISalaryHistoryRepository _salaryHistoryRepository;
    private readonly ILogger<PayrollRecordService> _logger;
    public PayrollRecordService(IPayrollRecordRepository payrollRecordRepository, IEmployeeRepository employeeRepository, ISalaryHistoryRepository salaryHistoryRepository,ILogger<PayrollRecordService> logger)
    {
        _payrollRecordRepository = payrollRecordRepository;
        _employeeRepository = employeeRepository;
        _salaryHistoryRepository = salaryHistoryRepository;
        _logger = logger;
    }

    
   public async Task GenerateMonthlyPayrollRecordsAsync()
{
    var employees = await _employeeRepository.GetActiveEmployeesAsync();

  
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var startOfMonth = new DateOnly(today.Year, today.Month, 1);
    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
    
    var existingPayrolls = await _payrollRecordRepository.GetPayrollRecordsAsync(
        new PayrollRecordFilter
        {
            FromDate = startOfMonth,
            ToDate = endOfMonth
        });
    var existingPayrollsList = existingPayrolls.ToList();
    foreach (var employee in employees)
    {
        if (existingPayrollsList.Any(p => p.EmployeeId == employee.Id))
            continue;

       
        var lastPayroll = await _payrollRecordRepository.GetLatestByEmployeeIdAsync(employee.Id);

        if (lastPayroll == null)
        {
            _logger.LogWarning("Skipping EmployeeId {Id}: no previous payroll found.", employee.Id);
            continue;
        }
        var latestSalary = await _salaryHistoryRepository.GetLatestSalaryHistoryAsync(employee.Id);
        if (latestSalary == null)
        {
            _logger.LogWarning("Skipping EmployeeId {Id}: no salary history found.", employee.Id);
            continue;
        }

    
        var newPayroll = new AddPayrollRecordDto()
        {
            EmployeeId = employee.Id,
            PeriodStart = startOfMonth,
            PeriodEnd = endOfMonth,
            Deductions = lastPayroll.Deductions, 
              };

        await AddPayrollRecordAsync(newPayroll);

        _logger.LogInformation("Auto-generated payroll for EmployeeId {Id} for {Month}", 
            employee.Id, startOfMonth.ToString("yyyy-MM"));
    }
    
}

   public async Task<Response<GetPayrollRecordDto>> AddPayrollRecordAsync(AddPayrollRecordDto payrollDto)
{
    var employee = await _employeeRepository.GetEmployeeByIdAsync(payrollDto.EmployeeId);
    if (employee is null)
    {
        return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, "Employee not found.");
    }

    var latestSalary = await _salaryHistoryRepository.GetLatestSalaryHistoryAsync(payrollDto.EmployeeId);
    if (latestSalary is null)
    {
        return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, "No Salary record found for this employee.");
    }


    var periodStart = latestSalary.Month;  
    var periodEnd = periodStart.AddMonths(1);

    var payroll = new Domain.Entities.PayrollRecord
    {
        EmployeeId = payrollDto.EmployeeId,
        PeriodStart = periodStart,
        PeriodEnd = periodEnd,
        GrossPay = latestSalary.ExpectedTotal,
        Deductions = payrollDto.Deductions,
        CreatedAt = DateTime.UtcNow
    };

    var isAdded = await _payrollRecordRepository.AddAsync(payroll);
    if (!isAdded)
    {
        return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, "Error while adding new payroll record.");
    }

    return new Response<GetPayrollRecordDto>(
        HttpStatusCode.OK,
        $"Payroll record for {employee.FirstName} was added successfully",
        new GetPayrollRecordDto
        {
            Id = payroll.Id,
            PeriodStart = payroll.PeriodStart,
            PeriodEnd = payroll.PeriodEnd,
            GrossPay = payroll.GrossPay,
            Deductions = payroll.Deductions,
            NetPay = payroll.NetPay,
            CreatedAt = payroll.CreatedAt,
            EmployeeId = payroll.EmployeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}"
        }
    );
}


    public async Task<Response<List<GetPayrollRecordDto>>> GetAllPayrollRecordsAsync()
    {
        var records = await _payrollRecordRepository.GetAllAsync();
        if (records.Count == 0)
        {
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, "No payroll records were found.");
        }

        var dto = records.Select(p => new GetPayrollRecordDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee.FirstName,
            CreatedAt = p.CreatedAt,
            GrossPay = p.GrossPay,
            Deductions = p.Deductions,
            NetPay = p.NetPay,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd
        }).ToList();

        return new Response<List<GetPayrollRecordDto>>(
            HttpStatusCode.OK, "Payroll records retrieved successfully!",dto);
    }

    public async Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id)
    {
        var record = await _payrollRecordRepository.GetByIdAsync(id);
        if (record is null)
        {
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, "Payroll record is not found.");
        }

        var mapped = new GetPayrollRecordDto
        {
          Id=record.Id,
          CreatedAt = record.CreatedAt,
          GrossPay = record.GrossPay,
          Deductions = record.Deductions,
          NetPay = record.NetPay,
          PeriodStart = record.PeriodStart,
          PeriodEnd = record.PeriodEnd,
          EmployeeId = record.EmployeeId,
          EmployeeName = record.Employee.FirstName
        };

        return new Response<GetPayrollRecordDto>(
            HttpStatusCode.OK, 
            "Payroll record is retrieved successfully!",
            mapped);

    }

    public async Task<Response<List<GetPayrollRecordDto>>> GetPayrollRecordsByEmployeeIdAsync(int employeeId)
    {
        if (await _employeeRepository.GetEmployeeByIdAsync(employeeId) is null)
        {
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, $"Employee with ID:{employeeId} not found.");

        }
        var record = await _payrollRecordRepository.GetByEmployeeIdAsync(employeeId);
        if (record.Count==0)
        {
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, $"Payroll records for employee with ID:{employeeId} are not found.");
        }

        var mapped = record.Select(r => new GetPayrollRecordDto
        {
            Id = r.Id,
            CreatedAt = r.CreatedAt,
            GrossPay = r.GrossPay,
            Deductions = r.Deductions,
            NetPay = r.NetPay,
            PeriodStart = r.PeriodStart,
            PeriodEnd = r.PeriodEnd,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee.FirstName
        }).ToList();
       

        return new Response<List<GetPayrollRecordDto>>(
            HttpStatusCode.OK, 
            "Payroll record is retrieved successfully!",
            mapped);

    }

    public async Task<Response<GetPayrollRecordDto>> GetLatestPayrollRecordByEmployeeIdAsync(int employeeId)
    {
        if (await _employeeRepository.GetEmployeeByIdAsync(employeeId) is null)
        {
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, $"Employee with ID:{employeeId} not found.");
        }

        var record = await _payrollRecordRepository.GetLatestByEmployeeIdAsync(employeeId);
       
        if (record is null)
        {
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, $"Latest record for employee with ID:{employeeId} not found.");
        }

        var mapped = new GetPayrollRecordDto
        {
            Id = record.Id,
            CreatedAt = record.CreatedAt,
            GrossPay = record.GrossPay,
            Deductions = record.Deductions,
            NetPay = record.NetPay,
            PeriodStart = record.PeriodStart,
            PeriodEnd = record.PeriodEnd,
            EmployeeId = record.EmployeeId,
            EmployeeName = record.Employee.FirstName
        };
        return new Response<GetPayrollRecordDto>(
            HttpStatusCode.OK,
            message: "Records for employee retrieved successfully",
            mapped);

    }

    public async Task<Response<bool>> UpdatePayrollRecordAsync(UpdatePayrollRecordDto payrollDto)
    {
        var exists = await _payrollRecordRepository.GetByIdAsync(payrollDto.Id);
        if (exists is null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, "Payroll record is not found.");
        }

        exists.Deductions = payrollDto.Deductions;

        var isUpdated = await _payrollRecordRepository.UpdateAsync(exists);
        if (isUpdated == false)
        {
            return new Response<bool>(HttpStatusCode.InternalServerError, "Payroll record could not be updated.");
        }

        return new Response<bool>(HttpStatusCode.OK, "Record is updated successfully!",true);

    }

    public async Task<Response<bool>> DeletePayrollRecordAsync(int id)
    {
        var isDeleted = await _payrollRecordRepository.DeleteAsync(id);
        if (isDeleted == false)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, "Failed to delete payroll record.");
        }

        return new Response<bool>(HttpStatusCode.OK, "Payroll record is deleted successfully", true);

    }
    
    //for bar chart
    public async Task<Response<List<MonthPayrollDto>>> GetPayrollForLastSixMonthAsync()
    {
        var result = new List<MonthPayrollDto>();
        var today = DateTime.Today;

        for (int i = 5; i >= 0; i--)
        {
            var month = new DateOnly(today.Year, today.Month, 1).AddMonths(-i);
            var total = await _payrollRecordRepository.GetTotalPaidForMonth(month);
            result.Add(new MonthPayrollDto
            {
                Month = month.ToString("MMM yyyy"), // e.g., "Nov 2025"
                TotalNetPay = total
            });
        }
        return new Response<List<MonthPayrollDto>>(HttpStatusCode.OK, result);
    }

    public async Task<Response<(Dictionary<string, decimal> GrossPayByMonth, Dictionary<string, decimal> NetPayByMonth)>> 
        GetPayrollSummaryAsync(DateTime startMonth, DateTime endMonth)
    {
        var payrollRecords = await _payrollRecordRepository.GetPayrollRecordsAsync(startMonth, endMonth);

        
        var grossPayByMonth = new Dictionary<string, decimal>();
        var netPayByMonth = new Dictionary<string, decimal>();
        
        foreach (var record in payrollRecords)
        {
            string monthKey = record.CreatedAt.ToString("yyyy-MM");

            if (!grossPayByMonth.ContainsKey(monthKey))
            {
                grossPayByMonth[monthKey] = 0;
                netPayByMonth[monthKey] = 0;
            }

            grossPayByMonth[monthKey] += Math.Round(record.GrossPay, 0, MidpointRounding.AwayFromZero);
            netPayByMonth[monthKey] += Math.Round(record.NetPay, 0, MidpointRounding.AwayFromZero);
        }

      
        var orderedGross = grossPayByMonth.OrderBy(k => k.Key)
            .ToDictionary(k => k.Key, v => v.Value);
        var orderedNet = netPayByMonth.OrderBy(k => k.Key)
            .ToDictionary(k => k.Key, v => v.Value);

       
        var data = (orderedGross, orderedNet);
        return new Response<(Dictionary<string, decimal>, Dictionary<string, decimal>)>(
            HttpStatusCode.OK, 
            "Payroll summary retrieved successfully", 
            data
        );
    }
    
    
}