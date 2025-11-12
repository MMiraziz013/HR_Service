using Clean.Domain.Enums;

namespace Clean.Application.Services.VacationBalance;

public static class VacationCalculator
{
    private static int GetUnpaidLeaveDays(Domain.Entities.Employee employee)
    {
        var latestBalance = employee.VacationBalances
            .OrderByDescending(vb => vb.Year)
            .FirstOrDefault();

        if (latestBalance is null)
        {
            return 0;
        }
        
        var unpaidDaysCount = employee.VacationRecords
            .Count(vr => vr is { Status: VacationStatus.Finished, Type: VacationType.Unpaid }
                         && vr.StartDate > latestBalance.PeriodStart
                         && vr.EndDate < latestBalance.PeriodEnd);

        return unpaidDaysCount;
    }

    /// <summary>
    /// The main function that returns the grand total of available
    /// vacation days for the given employee (taking into account sick leaves, vacations, years of experience etc.)
    /// for his new Vacation Balance, once a year.
    /// </summary>
    /// <param name="employee"></param>
    /// <returns>int TotalDaysPerYear</returns>
    public static int GetEntitlementDays(Domain.Entities.Employee employee)
    {
        var unpaidDaysCount = GetUnpaidLeaveDays(employee);
        var bonusDaysCount = GetBonusDaysByExperience(employee);
        var totalDays = 24 + bonusDaysCount;
        if (unpaidDaysCount > 15)
        {
            totalDays = (int)Math.Round(totalDays * (365 - unpaidDaysCount) / 365.0);
        }

        return totalDays;
    }

    public static int GetBonusDaysByExperience(Domain.Entities.Employee employee)
    {
        var years = DateTime.Today.Year - employee.HireDate.Year;

        return years switch
        {
            >= 20 => 10,
            >= 15 => 7,
            >= 10 => 5,
            >= 5 => 3,
            _ => 0
        };
    }
}