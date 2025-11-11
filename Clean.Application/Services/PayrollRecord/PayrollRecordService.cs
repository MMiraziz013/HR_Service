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
    private readonly ICacheService _cacheService;

    public PayrollRecordService(IPayrollRecordRepository payrollRecordRepository, 
        IEmployeeRepository employeeRepository,
        ISalaryHistoryRepository salaryHistoryRepository,
        ILogger<PayrollRecordService> logger,
        ICacheService cacheService)
    {
        _payrollRecordRepository = payrollRecordRepository;
        _employeeRepository = employeeRepository;
        _salaryHistoryRepository = salaryHistoryRepository;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Creates payroll record at the beginning of each month, for previous work month
    /// Deduction have default percent according to which they are calculated
    /// </summary> 
    public async Task GenerateMonthlyPayrollRecordsAsync()
    {
        try
        {
            var employees = await _employeeRepository.GetActiveEmployeesAsync();
            
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // This is the work month we are generating payroll for (e.g., if today is Nov 1, this is October)
            var workMonth = today.AddMonths(-1); 
            var startOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, 1);
            var endOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, DateTime.DaysInMonth(workMonth.Year, workMonth.Month));
            
            // Check existing payrolls to avoid duplicates
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
                
                // Check if the latest salary history matches the work month
                if (salary == null || salary.Month.Month != workMonth.Month || salary.Month.Year != workMonth.Year)
                {
                    _logger.LogWarning("Skipping EmployeeId {Id}: no salary history for work month {Month}.", 
                        employee.Id, workMonth.ToString("yyyy-MM"));
                    continue;
                }
                
                var payrollDto = new AddPayrollRecordDto { EmployeeId = employee.Id };
                
                // AddPayrollRecordAsync handles the core creation logic
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
            
            if (generatedCount > 0)
            {
                await InvalidatePayrollListCaches();
            }

            _logger.LogInformation("{Count} payroll records generated for work month {Month}.", 
                generatedCount, workMonth.ToString("yyyy-MM"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error occurred during GenerateMonthlyPayrollRecordsAsync.");
        }
    }


 public async Task<Response<GetPayrollRecordDto>> AddPayrollRecordAsync(AddPayrollRecordDto payrollDto)
{
    try
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var workMonth = today.AddMonths(-1);
        var startOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month, 1);
        var endOfWorkMonth = new DateOnly(workMonth.Year, workMonth.Month,
            DateTime.DaysInMonth(workMonth.Year, workMonth.Month));

        var employee = await _employeeRepository.GetEmployeeByIdAsync(payrollDto.EmployeeId);
        if (employee is null || !employee.IsActive)
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, "Employee not found.");


        var salary = await _salaryHistoryRepository.GetLatestSalaryHistoryAsync(payrollDto.EmployeeId);
        if (salary is null || salary.Month.Month != workMonth.Month || salary.Month.Year != workMonth.Year)
            return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest,
                $"No salary record found for {workMonth:MMMM yyyy}.");

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
            return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest,
                "Error while adding new payroll record.");

        await InvalidatePayrollListCaches();

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
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while adding a new payroll record for Employee ID {Id}.", payrollDto.EmployeeId);
        return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while adding payroll record.");
    }
}

    public async Task<Response<UpdatePayrollDto>> UpdatePayrollDeductionsAsync(UpdatePayrollDto dto)
    {
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(dto.EmployeeId);
            if (employee is null)
                return new Response<UpdatePayrollDto>(HttpStatusCode.NotFound, "Employee not found.");

            var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1); // Check against previous work month

            
            var payroll = await _payrollRecordRepository.GetPayrollByMonthAsync(dto.EmployeeId, currentMonth);
            if (payroll is null)
                return new Response<UpdatePayrollDto>(HttpStatusCode.NotFound, "Payroll record for current/previous month not found.");
            
            payroll.Deductions = dto.Deductions;

            var isUpdated = await _payrollRecordRepository.UpdateAsync(payroll);
            if (isUpdated == false)
                return new Response<UpdatePayrollDto>(HttpStatusCode.InternalServerError, "Failed to update payroll.");

            await InvalidatePayrollListCaches();
            await _cacheService.RemoveAsync($"payroll_record_{payroll.Id}");
            await _cacheService.RemoveAsync($"payroll_latest_employee_{dto.EmployeeId}");
            await _cacheService.RemoveAsync($"payroll_employee_{dto.EmployeeId}");
            
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating payroll deductions for Employee ID {Id}.", dto.EmployeeId);
            return new Response<UpdatePayrollDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while updating payroll deductions.");
        }
    }

    public async Task<Response<List<GetPayrollRecordDto>>> GetAllPayrollRecordsAsync()
    {
        const string cacheKey = "payroll_all_records";
        try
        {
            var cached = await _cacheService.GetAsync<Response<List<GetPayrollRecordDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            var records = await _payrollRecordRepository.GetAllAsync();
            if (records.Count == 0)
            {
                return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, "No payroll records were found.");
            }

            var dto = records.Select(p => new GetPayrollRecordDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeName = $"{p.Employee.FirstName} {p.Employee.LastName}", 
                CreatedAt = p.CreatedAt,
                GrossPay = p.GrossPay,
                Deductions = p.Deductions,
                NetPay = p.NetPay,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd
            }).ToList();

            var response = new Response<List<GetPayrollRecordDto>>(
                HttpStatusCode.OK, "Payroll records retrieved successfully!", dto);
            
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving all payroll records.");
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving all payroll records.");
        }
    }

    public async Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id)
    {
        var cacheKey = $"payroll_record_{id}";
        try
        {
            var cached = await _cacheService.GetAsync<Response<GetPayrollRecordDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
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
                EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}"
            };

            var response = new Response<GetPayrollRecordDto>(
                HttpStatusCode.OK, 
                "Payroll record is retrieved successfully!",
                mapped);
            
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving payroll record with ID {id}.", id);
            return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving payroll record.");
        }
    }

    public async Task<Response<List<GetPayrollWithSalaryDto>>> GetPayrollRecordsByEmployeeIdAsync(int employeeId)
    {
        var cacheKey = $"payroll_employee_{employeeId}";
        try
        {
            var cached = await _cacheService.GetAsync<Response<List<GetPayrollWithSalaryDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            if (employee is null)
            {
                return new Response<List<GetPayrollWithSalaryDto>>(HttpStatusCode.NotFound, $"Employee with ID:{employeeId} not found.");
            }
            
            var record = await _payrollRecordRepository.GetByEmployeeIdAsync(employeeId);
            if (!record.Any())
            {
                return new Response<List<GetPayrollWithSalaryDto>>(HttpStatusCode.NotFound, $"Payroll records for employee with ID:{employeeId} are not found.");
            }

            var salaryHistory = await _salaryHistoryRepository.GetSalaryHistoryByEmployeeIdAsync(employeeId);
            

            var mapped = record.Select(r =>
            {
                var salary = salaryHistory.FirstOrDefault(s =>
                    s.Month.Year == r.PeriodStart.Year &&
                    s.Month.Month == r.PeriodStart.Month);

                return new GetPayrollWithSalaryDto
                {
                    Id = r.Id,
                    CreatedAt = r.CreatedAt,
                    GrossPay = r.GrossPay,
                    Deductions = r.Deductions,
                    NetPay = r.NetPay,
                    PeriodStart = r.PeriodStart,
                    PeriodEnd = r.PeriodEnd,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = $"{r.Employee.FirstName} {r.Employee.LastName}",
                    BaseSalary = salary?.BaseAmount ?? 0,
                    Bonus = salary?.BonusAmount ?? 0
                };
            }).ToList();

            var response = new Response<List<GetPayrollWithSalaryDto>>(
                HttpStatusCode.OK, 
                "Payroll record is retrieved successfully!",
                mapped);
            
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving payroll records for Employee ID {id}.", employeeId);
            return new Response<List<GetPayrollWithSalaryDto>>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving employee payroll records.");
        }
    }

    public async Task<Response<GetPayrollWithSalaryDto>> GetLatestPayrollRecordByEmployeeIdAsync(int employeeId)
    {
        var cacheKey = $"payroll_latest_employee_{employeeId}";
        try
        {
            var cached = await _cacheService.GetAsync<Response<GetPayrollWithSalaryDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            if (await _employeeRepository.GetEmployeeByIdAsync(employeeId) is null)
            {
                return new Response<GetPayrollWithSalaryDto>(HttpStatusCode.NotFound, $"Employee with ID:{employeeId} not found.");
            }

            var record = await _payrollRecordRepository.GetLatestByEmployeeIdAsync(employeeId);
        
            if (record is null)
            {
                return new Response<GetPayrollWithSalaryDto>(HttpStatusCode.NotFound, $"Latest record for employee with ID:{employeeId} not found.");
            }

            var salaryHistory = await _salaryHistoryRepository.GetSalaryByMonth(employeeId, record.PeriodStart);
        
            var mapped = new GetPayrollWithSalaryDto()
            {
                Id = record.Id,
                CreatedAt = record.CreatedAt,
                GrossPay = record.GrossPay,
                Deductions = record.Deductions,
                NetPay = record.NetPay,
                PeriodStart = record.PeriodStart,
                PeriodEnd = record.PeriodEnd,
                EmployeeId = record.EmployeeId,
                // Ensure record.Employee is loaded/not null
                EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
                BaseSalary = salaryHistory?.BaseAmount ?? 0,
                Bonus = salaryHistory?.BonusAmount ?? 0
            };
            
            var response = new Response<GetPayrollWithSalaryDto>(
                HttpStatusCode.OK,
                message: "Records for employee retrieved successfully",
                mapped);
            
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving latest payroll record for Employee ID {id}.", employeeId);
            return new Response<GetPayrollWithSalaryDto>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving latest payroll record.");
        }
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
        try
        {
            var record = await _payrollRecordRepository.GetByIdAsync(id); // Retrieve for a cache key
            
            var isDeleted = await _payrollRecordRepository.DeleteAsync(id);
            if (isDeleted == false)
            {
                return new Response<bool>(HttpStatusCode.NotFound, "Failed to delete payroll record.");
            }

            await InvalidatePayrollListCaches();
            if (record != null)
            {
                await _cacheService.RemoveAsync($"payroll_record_{id}");
                await _cacheService.RemoveAsync($"payroll_employee_{record.EmployeeId}");
                await _cacheService.RemoveAsync($"payroll_latest_employee_{record.EmployeeId}");
            }


            return new Response<bool>(HttpStatusCode.OK, "Payroll record is deleted successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting payroll record with ID {id}.", id);
            return new Response<bool>(HttpStatusCode.InternalServerError, "Unexpected error occurred while deleting payroll record.");
        }
    }
    
    //for bar chart
    public async Task<Response<List<MonthPayrollDto>>> GetPayrollForLastSixMonthAsync()
    {
        const string cacheKey = "payroll_last_six_months";
        try
        {
            var cached = await _cacheService.GetAsync<Response<List<MonthPayrollDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
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
            
            var response = new Response<List<MonthPayrollDto>>(HttpStatusCode.OK, result);
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving payroll for the last six months.");
            return new Response<List<MonthPayrollDto>>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving monthly payroll summary.");
        }
    }

    public async Task<Response<List<PayrollGraphDto>>> GetPayrollSummaryAsync(int monthsRange)
    {
        string cacheKey = $"payroll_summary_graph_months_{monthsRange}";
        try
        {
            if (monthsRange <= 1 || monthsRange > 12)
                throw new ArgumentException("Invalid months range");

            var cached = await _cacheService.GetAsync<Response<List<PayrollGraphDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

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

            var response = new Response<List<PayrollGraphDto>>(
                HttpStatusCode.OK,
                message: "Total gross pay and net pay retrieved successfully",
                result);

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid month range supplied.");
            return new Response<List<PayrollGraphDto>>(HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving payroll summary.");
            return new Response<List<PayrollGraphDto>>(HttpStatusCode.InternalServerError, "Unexpected error occurred while retrieving payroll summary.");
        }
    }
    
    
    private async Task InvalidatePayrollListCaches()
    {
        await _cacheService.RemoveAsync("payroll_all_records");
        await _cacheService.RemoveAsync("payroll_last_six_months");
        await _cacheService.RemoveByPatternAsync("payroll_summary_graph_months_*");    
    }
}