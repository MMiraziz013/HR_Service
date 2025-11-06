using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationBalance;
using Clean.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.VacationBalance;

public class VacationBalanceService : IVacationBalanceService
{
    private readonly IVacationBalanceRepository _vacationBalanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IVacationRecordRepository _vacationRecordRepository;
    private readonly IDataContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<VacationBalanceService> _logger;

    public VacationBalanceService(
        IVacationBalanceRepository vacationBalanceRepository,
        IEmployeeRepository employeeRepository,
        IVacationRecordRepository vacationRecordRepository,
        IDataContext context,
        ICacheService cacheService,
        ILogger<VacationBalanceService> logger)
    {
        _vacationBalanceRepository = vacationBalanceRepository;
        _employeeRepository = employeeRepository;
        _vacationRecordRepository = vacationRecordRepository;
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    
    /// <summary>
    /// Used in the quartz.NET to automatically update VacationBalance
    /// of employees who have exactly +1 worked year since their hire date
    /// (Creates new VacationBalance record for each employee in the database according to their experience years)
    /// </summary>
    public async Task AutoUpdateVacationBalancesAsync()
    {
        var employees = await _employeeRepository.GetActiveEmployeesAsync();
        var existingBalances = await _vacationBalanceRepository.GetVacationBalancesAsync(new VacationBalanceFilter());

        foreach (var employee in employees)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var yearsWorked = today.Year - employee.HireDate.Year;
            if (yearsWorked <= 0) continue;

            var anniversary = employee.HireDate.AddYears(yearsWorked);
            if (anniversary != today) continue;

            if (existingBalances.Any(vb => vb.EmployeeId == employee.Id && vb.Year == anniversary.Year))
                continue;

            var periodStart = anniversary;
            var periodEnd = periodStart.AddYears(1).AddDays(-1);

            var dto = new AddVacationBalanceDto
            {
                EmployeeId = employee.Id,
                TotalDaysPerYear = VacationCalculator.GetEntitlementDays(employee),
                ByExperienceBonusDays = VacationCalculator.GetBonusDaysByExperience(employee),
                UsedDays = 0,
                Year = anniversary.Year,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };

            await AddVacationBalanceAsync(dto);
            _logger.LogInformation("Auto-created new vacation balance for EmployeeId {Id} for year {Year}", employee.Id, anniversary.Year);
        }
    }

    /// <summary>
    /// Automatically marks vacations that have ended as Finished.
    /// </summary>
    public async Task AutoUpdateVacationStatusesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var vacationsToFinish = await _vacationRecordRepository.GetVacationsToFinishAsync(today);

        if (vacationsToFinish.Count == 0)
        {
            _logger.LogInformation("No vacation records to finish as of {date}.", today);
            return;
        }

        foreach (var vacation in vacationsToFinish.Where(v=> v.Status != VacationStatus.Finished))
        {
            vacation.Status = VacationStatus.Finished;
        }

        await _context.SaveChangesAsync(CancellationToken.None);
        _logger.LogInformation("Marked {count} vacation records as Finished.", vacationsToFinish.Count);
    }

    public async Task<Response<GetVacationBalanceDto>> AddVacationBalanceAsync(AddVacationBalanceDto dto)
    {
        var exists = await _vacationBalanceRepository.ExistsAsync(dto.EmployeeId, dto.Year);
        if (exists)
        {
            return new Response<GetVacationBalanceDto>(HttpStatusCode.BadRequest,
                "Vacation Balance for this employee already exists for this working year.");
        }
        
        var vBalance = new Domain.Entities.VacationBalance
        {
            TotalDaysPerYear = dto.TotalDaysPerYear,
            UsedDays = dto.UsedDays,
            Year = dto.Year,
            ByExperienceBonusDays = dto.ByExperienceBonusDays,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            EmployeeId = dto.EmployeeId,
        };

        var isAdded = await _vacationBalanceRepository.AddVacationBalanceAsync(vBalance);

        if (isAdded == false)
        {
            return new Response<GetVacationBalanceDto>(HttpStatusCode.BadRequest,
                "Couldn't add the vacation record, error...");
        }

        var employee = await _employeeRepository.GetEmployeeByIdAsync(vBalance.EmployeeId);
        
        return new Response<GetVacationBalanceDto>(HttpStatusCode.OK, new GetVacationBalanceDto
        {
            Id = vBalance.Id,
            TotalDaysPerYear = vBalance.TotalDaysPerYear,
            UsedDays = vBalance.UsedDays,
            Year = vBalance.Year,
            ByExperienceBonusDays = vBalance.ByExperienceBonusDays,
            PeriodStart = vBalance.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEnd = vBalance.PeriodEnd.ToString("yyyy-MM-dd"),
            EmployeeId = vBalance.EmployeeId,
            Employee = new GetEmployeeDto
            {
                Id = employee!.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Position = employee.Position,
                HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
                BaseSalary = employee.SalaryHistories.OrderByDescending(sh => sh.Month).Select(sh => sh.BaseAmount)
                    .FirstOrDefault(),
                IsActive = employee.IsActive,
                DepartmentName = employee.Department.Name
            }
        });
    }

    public async Task<Response<List<GetVacationBalanceDto>>> GetAllVacationBalancesAsync(VacationBalanceFilter filter)
    {
        var vacationTables = await _vacationBalanceRepository.GetVacationBalancesAsync(filter);
        var allBalances = vacationTables.Select(vb => new GetVacationBalanceDto
        {
            Id = vb.Id,
            TotalDaysPerYear = vb.TotalDaysPerYear,
            UsedDays = vb.UsedDays,
            Year = vb.Year,
            ByExperienceBonusDays = vb.ByExperienceBonusDays,
            PeriodStart = vb.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEnd = vb.PeriodEnd.ToString("yyyy-MM-dd"),
            EmployeeId = vb.EmployeeId,
            Employee = new GetEmployeeDto
            {
                Id = vb.Employee!.Id,
                FirstName = vb.Employee.FirstName,
                LastName = vb.Employee.LastName,
                Position = vb.Employee.Position,
                HireDate = vb.Employee.HireDate.ToString("yyyy-MM-dd"),
                BaseSalary = vb.Employee.SalaryHistories.OrderByDescending(sh => sh.Month).Select(sh => sh.BaseAmount)
                    .FirstOrDefault(),
                IsActive = vb.Employee.IsActive,
                DepartmentName = vb.Employee.Department.Name
            },
        }).ToList();

        return new Response<List<GetVacationBalanceDto>>(HttpStatusCode.OK, allBalances);
    }

    public async Task<Response<List<GetVacationBalanceDto>>> GetLatestVacationBalancesAsync(VacationBalanceFilter filter)
    {
        var activeEmployees = await _employeeRepository.GetActiveEmployeesAsync();

        var latestBalances = activeEmployees
            .Where(e => e.VacationBalances.Count != 0)
            .Select(e =>
            {
                var latest = e.VacationBalances
                    .OrderByDescending(vb => vb.Year)
                    .First();

                return new GetVacationBalanceDto
                {
                    Id = latest.Id,
                    TotalDaysPerYear = latest.TotalDaysPerYear,
                    UsedDays = latest.UsedDays,
                    Year = latest.Year,
                    ByExperienceBonusDays = latest.ByExperienceBonusDays,
                    PeriodStart = latest.PeriodStart.ToString("yyyy-MM-dd"),
                    PeriodEnd = latest.PeriodEnd.ToString("yyyy-MM-dd"),
                    EmployeeId = latest.EmployeeId,
                    Employee = new GetEmployeeDto
                    {
                        Id = e.Id,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        Position = e.Position,
                        HireDate = e.HireDate.ToString("yyyy-MM-dd"),
                        BaseSalary = e.SalaryHistories.OrderByDescending(sh=> sh.Month).Select(sh=> sh.BaseAmount).FirstOrDefault(),
                        IsActive = e.IsActive,
                        DepartmentName = e.Department.Name
                    }
                };
            })
            .ToList();

        return new Response<List<GetVacationBalanceDto>>(HttpStatusCode.OK, latestBalances);
    }
    
    public async Task<Response<GetVacationBalanceDto>> GetVacationBalanceByEmployeeIdAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee is null)
        {
            return new Response<GetVacationBalanceDto>(HttpStatusCode.BadRequest, "No such employee in the system");
        }

        var employeeBalance = employee.VacationBalances.OrderByDescending(vb => vb.Year).FirstOrDefault();
        var eBalance = new GetVacationBalanceDto
        {
            Id = employeeBalance!.Id,
            TotalDaysPerYear = employeeBalance.TotalDaysPerYear,
            UsedDays = employeeBalance.UsedDays,
            Year = employeeBalance.Year,
            ByExperienceBonusDays = employeeBalance.ByExperienceBonusDays,
            PeriodStart = employeeBalance.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEnd = employeeBalance.PeriodEnd.ToString("yyyy-MM-dd"),
            EmployeeId = employeeBalance.EmployeeId,
            Employee = new GetEmployeeDto
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Position = employee.Position,
                HireDate = employee.HireDate.ToString("yyyy-MM-dd"),
                BaseSalary = employee.SalaryHistories.OrderByDescending(sh=> sh.Month).Select(sh=> sh.BaseAmount).FirstOrDefault(),
                IsActive = employee.IsActive,
                DepartmentName = employee.Department.Name
            }
        };

        return new Response<GetVacationBalanceDto>(HttpStatusCode.OK, eBalance);
    }

    public async Task<Response<GetVacationBalanceDto>> GetVacationBalanceByIdAsync(int vacationBalanceId)
    {
        var vBalance = await _vacationBalanceRepository.GetVacationBalanceByIdAsync(vacationBalanceId);
        if (vBalance is null)
        {
            return new Response<GetVacationBalanceDto>(HttpStatusCode.BadRequest,
                "No such vacation balance in the system");
        }

        var vacationBalance = new GetVacationBalanceDto
        {
            Id = vBalance.Id,
            TotalDaysPerYear = vBalance.TotalDaysPerYear,
            UsedDays = vBalance.UsedDays,
            Year = vBalance.Year,
            ByExperienceBonusDays = vBalance.ByExperienceBonusDays,
            PeriodStart = vBalance.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEnd = vBalance.PeriodEnd.ToString("yyyy-MM-dd"),
            EmployeeId = vBalance.EmployeeId,
            Employee = new GetEmployeeDto
            {
                Id = vBalance.Employee.Id,
                FirstName = vBalance.Employee.FirstName,
                LastName = vBalance.Employee.LastName,
                Position = vBalance.Employee.Position,
                HireDate = vBalance.Employee.HireDate.ToString("yyyy-MM-dd"),
                BaseSalary = vBalance.Employee.SalaryHistories.OrderByDescending(sh=> sh.Month).Select(sh=> sh.BaseAmount).FirstOrDefault(),
                IsActive = vBalance.Employee.IsActive,
                DepartmentName = vBalance.Employee.Department.Name
            }
        };

        return new Response<GetVacationBalanceDto>(HttpStatusCode.OK, vacationBalance);
    }

    public async Task<Response<GetVacationBalanceDto>> UpdateVacationBalanceAsync(UpdateVacationBalanceDto dto)
    {
        var recordToUpdate = await _vacationBalanceRepository.GetVacationBalanceByIdAsync(dto.Id);
        if (recordToUpdate is null)
        {
            return new Response<GetVacationBalanceDto>(HttpStatusCode.BadRequest,
                "No such vacation balance to update in the system");
        }

        if (dto.TotalDaysPerYear.HasValue)
        {
            recordToUpdate.TotalDaysPerYear = dto.TotalDaysPerYear.Value;
        }

        if (dto.ByExperienceBonusDays.HasValue)
        {
            recordToUpdate.ByExperienceBonusDays = dto.ByExperienceBonusDays.Value;
        }

        if (dto.PeriodStart.HasValue)
        {
            recordToUpdate.PeriodStart = dto.PeriodStart.Value;
        }

        if (dto.PeriodEnd.HasValue)
        {
            recordToUpdate.PeriodEnd = dto.PeriodEnd.Value;
        }

        if (dto.UsedDays.HasValue)
        {
            recordToUpdate.UsedDays = dto.UsedDays.Value;
        }

        if (dto.Year.HasValue)
        {
            recordToUpdate.Year = dto.Year.Value;
        }

        var updatedBalance = await _vacationBalanceRepository.UpdateVacationBalanceAsync(recordToUpdate);
        
        if (updatedBalance == null)
        {
            return new Response<GetVacationBalanceDto>(HttpStatusCode.InternalServerError, "Failed to update employee");
        }

        var updatedVacationBalance = new GetVacationBalanceDto
        {
            Id = updatedBalance.Id,
            TotalDaysPerYear = updatedBalance.TotalDaysPerYear,
            UsedDays = updatedBalance.UsedDays,
            Year = updatedBalance.Year,
            ByExperienceBonusDays = updatedBalance.ByExperienceBonusDays,
            PeriodStart = updatedBalance.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEnd = updatedBalance.PeriodEnd.ToString("yyyy-MM-dd"),
            EmployeeId = updatedBalance.EmployeeId,
            Employee = new GetEmployeeDto
            {
                Id = updatedBalance.Employee.Id,
                FirstName = updatedBalance.Employee.FirstName,
                LastName = updatedBalance.Employee.LastName,
                Position = updatedBalance.Employee.Position,
                HireDate = updatedBalance.Employee.HireDate.ToString("yyyy-MM-dd"),
                BaseSalary = updatedBalance.Employee.SalaryHistories.OrderByDescending(sh=> sh.Month).Select(sh=> sh.BaseAmount).FirstOrDefault(),
                IsActive = updatedBalance.Employee.IsActive,
                DepartmentName = updatedBalance.Employee.Department.Name
            }
        };

        return new Response<GetVacationBalanceDto>(HttpStatusCode.OK, updatedVacationBalance);
    }
    
    //TODO: Check later if we need delete method for VacationBalance
}