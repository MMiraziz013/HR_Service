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

    /// <summary>
    /// Creates payroll record at the beginning of each month, for previous work month
    /// Deduction have default percent according to which they are calculated
    /// </summary> 
  public async Task GenerateMonthlyPayrollRecordsAsync()
{
    var employees = await _employeeRepository.GetActiveEmployeesAsync();
    
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    
    var workMonth = today.AddMonths(-1);
    var startOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, 1);
    var endOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, DateTime.DaysInMonth(workMonth.Year, workMonth.Month));
    
    var existingPayrolls = await _payrollRecordRepository.GetPayrollRecordsAsync(
        new PayrollRecordFilter
        {
            FromDate = startOfWorkMonth,
            ToDate = endOfWorkMonth
        });
    var existingPayrollsList = existingPayrolls.ToList();

    int generatedCount = 0;

    foreach (var employee in employees)
    {
        if (existingPayrollsList.Any(p => p.EmployeeId == employee.Id))
            continue;
        

    
        var salary = await _salaryHistoryRepository.GetLatestSalaryHistoryAsync(employee.Id);
        if (salary == null || salary.Month.Month != workMonth.Month || salary.Month.Year != workMonth.Year)
        {
            _logger.LogWarning("Skipping EmployeeId {Id}: no salary history for work month {Month}.", 
                employee.Id, workMonth.ToString("yyyy-MM"));
            continue;
        }
        
        var payrollDto = new AddPayrollRecordDto
        {
            EmployeeId = employee.Id,
        };
        
        var response = await AddPayrollRecordAsync(payrollDto);

        if (response.StatusCode == (int)HttpStatusCode.OK)
        {
            generatedCount++;
            _logger.LogInformation("Payroll generated for EmployeeId {Id} for work month {Month}.", 
                employee.Id, workMonth.ToString("yyyy-MM"));
        }
        else
        {
            _logger.LogWarning("Failed to generate payroll for EmployeeId {Id}: {Message}", 
                employee.Id, response.Message);
        }
    }

    _logger.LogInformation("{Count} payroll records generated for work month {Month}.", 
        generatedCount, workMonth.ToString("yyyy-MM"));
    
}



 public async Task<Response<GetPayrollRecordDto>> AddPayrollRecordAsync(AddPayrollRecordDto payrollDto)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var workMonth = today.AddMonths(-1); 
    var startOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, 1);
    var endOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, DateTime.DaysInMonth(workMonth.Year, workMonth.Month));
    
    var employee = await _employeeRepository.GetEmployeeByIdAsync(payrollDto.EmployeeId);
    if (employee is null || !employee.IsActive)
        return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, "Employee not found.");

    
    var salary = await _salaryHistoryRepository.GetLatestSalaryHistoryAsync(payrollDto.EmployeeId);
    if (salary is null || salary.Month.Month != workMonth.Month || salary.Month.Year != workMonth.Year)
        return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, $"No salary record found for {workMonth:MMMM yyyy}.");
    
    var periodStart = employee.HireDate > startOfWorkMonth ? employee.HireDate : startOfWorkMonth;
    var periodEnd = endOfWorkMonth;
    
    decimal grossPay;
    if (employee.HireDate > startOfWorkMonth)
    {
        int totalDays = DateTime.DaysInMonth(workMonth.Year, workMonth.Month);
        int workedDays = periodEnd.Day - periodStart.Day + 1;
        grossPay = salary.ExpectedTotal * workedDays / totalDays;
    }
    else
    {
        grossPay = salary.ExpectedTotal;
    }

   
    decimal defaultDeduction = Math.Round(grossPay * 0.03m, 2);

    // Combine with any manual HR deductions (if already passed)

    

    var payroll = new Domain.Entities.PayrollRecord
    {
        EmployeeId = payrollDto.EmployeeId,
        PeriodStart = periodStart,
        PeriodEnd = periodEnd,
        GrossPay = grossPay,
        Deductions = defaultDeduction,
        CreatedAt = DateTime.UtcNow
    };

    var isAdded = await _payrollRecordRepository.AddAsync(payroll);
    if (!isAdded)
        return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, "Error while adding new payroll record.");

    return new Response<GetPayrollRecordDto>(
        HttpStatusCode.OK,
        $"Payroll record for {employee.FirstName} {employee.LastName} generated automatically for {workMonth:MMMM yyyy}.",
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

public async Task<Response<UpdatePayrollDto>> UpdatePayrollDeductionsAsync(UpdatePayrollDto dto)
{
    var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
    if (employee is null)
        return new Response<UpdatePayrollDto>(HttpStatusCode.NotFound, "Employee not found.");

    var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

    
    var payroll = await _payrollRecordRepository.GetPayrollByMonthAsync(dto.EmployeeId, currentMonth);
    if (payroll is null)
        return new Response<UpdatePayrollDto>(HttpStatusCode.NotFound, "Payroll record for current month not found.");
    
    payroll.Deductions = dto.Deductions;

    var isUpdated = await _payrollRecordRepository.UpdateAsync(payroll);
    if (!isUpdated)
        return new Response<UpdatePayrollDto>(HttpStatusCode.InternalServerError, "Failed to update payroll.");

    var updatedDto = new UpdatePayrollDto
    {
        EmployeeId = payroll.EmployeeId,
        Deductions = payroll.Deductions,
    };

    return new Response<UpdatePayrollDto>(
        HttpStatusCode.OK,
        "Payroll deductions updated successfully.",
        updatedDto
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

    // public async Task<Response<bool>> UpdatePayrollRecordAsync(UpdatePayrollRecordDto payrollDto)
    // {
    //     var exists = await _payrollRecordRepository.GetByIdAsync(payrollDto.Id);
    //     if (exists is null)
    //     {
    //         return new Response<bool>(HttpStatusCode.NotFound, "Payroll record is not found.");
    //     }
    //
    //     exists.Deductions = payrollDto.Deductions;
    //
    //     var isUpdated = await _payrollRecordRepository.UpdateAsync(exists);
    //     if (isUpdated == false)
    //     {
    //         return new Response<bool>(HttpStatusCode.InternalServerError, "Payroll record could not be updated.");
    //     }
    //
    //     return new Response<bool>(HttpStatusCode.OK, "Record is updated successfully!",true);
    //
    // }

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
        var lastMonth = DateTime.Today.AddMonths(-1);

        for (int i = 5; i >= 0; i--)
        {
            var month = new DateOnly(lastMonth.Year, lastMonth.Month, 1).AddMonths(-i);
            var total = await _payrollRecordRepository.GetTotalPaidForMonth(month);
            result.Add(new MonthPayrollDto
            {
                Month = month.ToString("MMM yyyy"), 
                TotalNetPay = total
            });
        }
        return new Response<List<MonthPayrollDto>>(HttpStatusCode.OK, result);
    }

    public async  Task<Response<List<PayrollGraphDto>>> GetPayrollSummaryAsync(int monthsRange)
    {
        if (monthsRange <= 1 || monthsRange > 12)
            throw new ArgumentException("Invalid months range");

        var result = new List<PayrollGraphDto>();
        var lastMonth = DateTime.Today.AddMonths(-1);

        for (int i = monthsRange - 1; i >= 0; i--)
        {
            var month = new DateOnly(lastMonth.Year, lastMonth.Month, 1).AddMonths(-i);
            
            var payrolls = await _payrollRecordRepository
                .GetPayrollRecordsByMonthAsync(month.Year, month.Month);

            var totalNetPay = payrolls.Sum(p => p.NetPay);
            var totalGrossPay = payrolls.Sum(p => p.GrossPay);

            result.Add(new PayrollGraphDto
            {
                Month = month.ToString("MMM yyyy"),
                TotalNetPay = totalNetPay,
                TotalGrossPay = totalGrossPay
            });
        }

        return new Response<List<PayrollGraphDto>>(
            HttpStatusCode.OK,
            message: "Total gross pay and net pay retrieved successfully",
            result);
    }
    
    
}