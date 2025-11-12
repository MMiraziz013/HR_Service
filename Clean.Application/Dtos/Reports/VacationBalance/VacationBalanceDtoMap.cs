using CsvHelper.Configuration;

namespace Clean.Application.Dtos.Reports.VacationBalance;

public sealed class VacationBalanceDtoMap : ClassMap<VacationBalanceDto>
{
    public VacationBalanceDtoMap()
    {
        Map(vb => vb.Id).Name("Vacation ID");
        Map(vb => vb.EmployeeId).Name("Employee ID");
        Map(vb => vb.EmployeeName).Name("Employee Name");
        Map(vb => vb.DepartmentName).Name("Department");
        Map(vb => vb.Position).Name("Position");
        Map(vb => vb.Role).Name("Role");
        Map(vb => vb.WorkedYears).Name("Years Worked");
        
        Map(vb => vb.DaysPerYear).Name("Standard Days per Year");
        Map(vb => vb.ByExperienceBonusDays).Name("Bonus Days (by experience)");
        Map(vb => vb.TotalDaysPerYear).Name("Total Days per Year");
        Map(vb => vb.UsedDays).Name("Used Days");
        Map(vb => vb.RemainingDays).Name("Remaining Days");
        Map(vb => vb.VacationsTaken).Name("Vacations Taken");
        Map(vb => vb.Year);

        Map(vb => vb.HasBonusDays).Name("Has Bonus Days");
        Map(vb => vb.HasUsedDays).Name("Has Used Days");
        Map(vb => vb.IsLimitFinished).Name("Is Vacation Limit Finished");
        
        Map(vb => vb.BalanceFrom).Name("Period From");
        Map(vb => vb.BalanceTo).Name("Period To");
    }
}