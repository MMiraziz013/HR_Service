using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VacationBalance;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.VacationBalance;

public class VacationBalanceService : IVacationBalanceService
{
    private readonly IVacationBalanceRepository _vacationBalanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<VacationBalanceService> _logger;

    public VacationBalanceService(
        IVacationBalanceRepository vacationBalanceRepository,
        IEmployeeRepository employeeRepository,
        ILogger<VacationBalanceService> logger)
    {
        _vacationBalanceRepository = vacationBalanceRepository;
        _employeeRepository = employeeRepository;
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

    
    public async Task<Response<GetVacationBalanceDto>> AddVacationBalanceAsync(AddVacationBalanceDto dto)
    {
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
        };

        return new Response<GetVacationBalanceDto>(HttpStatusCode.OK, updatedVacationBalance);
    }
    
    //TODO: Check later if we need delete method for VacationBalance
}