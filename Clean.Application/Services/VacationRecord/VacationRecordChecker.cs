using Clean.Application.Abstractions;
using Clean.Application.Dtos.VacationRecords;
using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Services.VacationRecord;

public class VacationRecordChecker
{
    public static VacationCheckDto CheckVacation(RequestVacationDto request, Domain.Entities.Employee employee)
    {
        decimal paymentAmount = 0;
        
        var pastDateCheck = CheckStartDate(request);
        if (pastDateCheck.IsAvailable == false)
        {
            return pastDateCheck;
        }

        var durationCheck = CheckMaxDuration(request);
        if (durationCheck.IsAvailable == false)
        {
            return durationCheck;
        }

        var experienceCheck = CheckEmploymentDuration(employee);
        if (experienceCheck.IsAvailable == false)
        {
            return experienceCheck;
        }

        var overlapCheck = CheckOverlap(request, employee);
        if (overlapCheck.IsAvailable == false)
        {
            return overlapCheck;
        }

        var gapCheck = CheckTimeGapSinceLastVacation(employee);
        if (gapCheck.IsAvailable == false)
        {
            return gapCheck;
        }

        if (request.Type == VacationType.Paid)
        {
            var balanceCheck = CheckVacationBalance(request, employee);
            if (balanceCheck.IsAvailable == false)
            {
                return balanceCheck;
            }

            // Payment Amount: Payment calculation logic
            var paymentCheck = CalculatePaymentAmount(request, employee);
            if (paymentCheck.IsAvailable == false)
            {
                return paymentCheck;
            }

            paymentAmount = paymentCheck.PaymentAmount;
        }

        return new VacationCheckDto
        {
            IsAvailable = true,
            Message = "Vacation request is valid and can be submitted.",
            PaymentAmount = paymentAmount
        };
    }

    // -----------------------------------------------
    // 🧩 Individual Validation Methods
    // -----------------------------------------------

    /// <summary>
    /// Checks if the start date of the requested vacation is at least 7 days from today.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private static VacationCheckDto CheckStartDate(RequestVacationDto request)
    {
        if (request.StartDate < DateOnly.FromDateTime(DateTime.Today.AddDays(7)))
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = "Vacation should be requested at least 7 days in advance."
            };
        }

        return Success();
    }

    /// <summary>
    /// Checks if the max duration of the requested vacation is not more than 24 days.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private static VacationCheckDto CheckMaxDuration(RequestVacationDto request)
    {
        var duration = (request.EndDate.ToDateTime(TimeOnly.MinValue) - request.StartDate.ToDateTime(TimeOnly.MinValue)).Days;

        if (duration > 24)
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = "You can take up to 24 days per vacation.",
            };
        }

        return Success();
    }

    /// <summary>
    /// Checks if employee has worked at least 6 months in the company.
    /// </summary>
    /// <param name="employee"></param>
    /// <returns></returns>
    private static VacationCheckDto CheckEmploymentDuration(Domain.Entities.Employee employee)
    {
        if (employee.HireDate.AddMonths(6) > DateOnly.FromDateTime(DateTime.Today))
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = "You must complete at least 6 months of employment before requesting vacation."
            };
        }

        return Success();
    }

    /// <summary>
    /// Checks if requested vacation is not overlapping with another vacation of this employee.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="employee"></param>
    /// <returns></returns>
    private static VacationCheckDto CheckOverlap(RequestVacationDto request, Domain.Entities.Employee employee)
    {
        bool overlaps = employee.VacationRecords.Any(v =>
            request.StartDate <= v.EndDate && request.EndDate >= v.StartDate
        );

        if (overlaps)
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = "Requested vacation overlaps with an existing vacation."
            };
        }

        return Success();
    }

    /// <summary>
    /// Checks if at least 6 months have passed since the last vacation of an employee.
    /// </summary>
    /// <param name="employee"></param>
    /// <returns></returns>
    private static VacationCheckDto CheckTimeGapSinceLastVacation(Domain.Entities.Employee employee)
    {
        var latestVacation = employee.VacationRecords
            .OrderByDescending(v => v.EndDate)
            .FirstOrDefault();

        if (latestVacation != null &&
            latestVacation.EndDate.AddMonths(5) > DateOnly.FromDateTime(DateTime.Today))
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = "It’s been less than 5 months since your last vacation."
            };
        }

        return Success();
    }

    /// <summary>
    /// Checks if an employee has enough days in their vacation balance, for their vacation being requested.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="employee"></param>
    /// <returns></returns>
    private static VacationCheckDto CheckVacationBalance(RequestVacationDto request, Domain.Entities.Employee employee)
    {
        var balance = employee.VacationBalances
            .OrderByDescending(vb => vb.Year)
            .FirstOrDefault();

        if (balance == null)
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = "Vacation balance not found for this employee."
            };
        }

        var requestedDays = (request.EndDate.ToDateTime(TimeOnly.MinValue) -
                             request.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
        
        if (requestedDays > balance.RemainingDays)
        {
            return new VacationCheckDto
            {
                IsAvailable = false,
                Message = $"You requested {requestedDays} days, but only {balance.RemainingDays} are available."
            };
        }

        return Success();
    }

    private static VacationCheckDto CalculatePaymentAmount(RequestVacationDto request, Domain.Entities.Employee employee)
    {
        if (employee.PayrollRecords.Count == 0)
        {
            return Fail("No payroll records found for employee.");
        }

        // The last 12 months of payroll
        var recentPayrolls = employee.PayrollRecords
            .OrderByDescending(pr => pr.PeriodStart)
            .Take(12)
            .ToList();

        if (recentPayrolls.Count == 0)
        {
            return Fail("You have no payroll data in the last 12 months.");
        }

        // Compute total income
        var totalIncome = recentPayrolls.Sum(pr => pr.NetPay);

        // Determine the total number of days covered by those payrolls
        var firstPeriodStart = recentPayrolls.Last().PeriodStart;
        var lastPeriodEnd = recentPayrolls.First().PeriodEnd; // assuming you have PeriodEnd in PayrollRecord

        var daysWorked = (lastPeriodEnd.ToDateTime(TimeOnly.MinValue) -
                          firstPeriodStart.ToDateTime(TimeOnly.MinValue)).Days + 1;

        var unpaidDays = employee.VacationRecords
            .Where(vr => vr.Type == VacationType.Unpaid && vr.EndDate >= firstPeriodStart)
            .Sum(vr => vr.DaysCount);

        var correctedDays = daysWorked - (unpaidDays > 15 ? unpaidDays : 0);
        if (correctedDays <= 0)
        {
            return Fail("Invalid corrected period for calculation.");
        }
        
        var averageDailyEarnings = totalIncome / correctedDays;

        // If vacation request is of type unpaid
        if (request.Type == VacationType.Unpaid)
        {
            return new VacationCheckDto
            {
                IsAvailable = true,
                Message = "Unpaid vacation — no payment amount calculated.",
                PaymentAmount = 0
            };
        }

        var totalPaidDays = (request.EndDate.ToDateTime(TimeOnly.MinValue) -
                             request.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

        var vacationPayment = averageDailyEarnings * totalPaidDays;

        return new VacationCheckDto
        {
            IsAvailable = true,
            Message = $"Calculated payment for {totalPaidDays} paid days. " +
                      $"Applied correction for {unpaidDays} unpaid leave days (>15 threshold).",
            PaymentAmount = Math.Round(vacationPayment, 2)
        };
    }
        

    // Result helper methods
    // ---------------------
    private static VacationCheckDto Success(string? message = null)
    {
        return new VacationCheckDto
        {
            IsAvailable = true,
            Message = message ?? string.Empty,
        };
    }

    private static VacationCheckDto Fail(string message)
    {
        return new VacationCheckDto
        {
            IsAvailable = false,
            Message = message,
            PaymentAmount = 0
        };
    }
}
