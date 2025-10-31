using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Seed;

public class SeedEmployeeUsers : IDataSeeder
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly DataContext _context;

    public SeedEmployeeUsers(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        DataContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task SeedAsync()
    {
        const string employeeRole = "Employee";
        const string defaultPassword = "Emp@12345";

        // Ensure Employee role exists
        if (!await _roleManager.RoleExistsAsync(employeeRole))
            await _roleManager.CreateAsync(new IdentityRole<int>(employeeRole));

        var employeesToSeed = new List<(string Email, string Username, string FirstName, string LastName)>
        {
            ("junior1@company.com", "junior1", "Alex", "Brown"),
            ("junior2@company.com", "junior2", "Emma", "Wilson")
        };

        // Get the first available department
        var department = await _context.Departments.FirstOrDefaultAsync();
        if (department == null)
            return; // No department to assign employees to — skip

        foreach (var (email, username, firstName, lastName) in employeesToSeed)
        {
            var user = await _userManager.FindByEmailAsync(email);
            Employee employee;

            if (user == null)
            {
                user = new User
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true,
                    Role = UserRole.Employee,
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, defaultPassword);
                if (!result.Succeeded)
                    continue;

                await _userManager.AddToRoleAsync(user, employeeRole);

                employee = new Employee
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Position = EmployeePosition.Junior,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    IsActive = true,
                    DepartmentId = department.Id,
                    UserId = user.Id
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
            }
            else
            {
                employee = (await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id))!;
                if (employee == null)
                    continue;
            }

            // Always ensure salary exists for current month
            var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var hasSalary = await _context.SalaryHistories
                .AnyAsync(s => s.EmployeeId == employee.Id && s.Month == currentMonth);

            if (!hasSalary)
            {
                var salary = new SalaryHistory
                {
                    EmployeeId = employee.Id,
                    Month = currentMonth,
                    BaseAmount = 1500,
                    BonusAmount = 1500 * 0.15m
                };

                _context.SalaryHistories.Add(salary);
                await _context.SaveChangesAsync();
            }
            
            var hireDate = employee.HireDate;
            var nextYearEnd = hireDate.AddYears(1).AddDays(-1);

            var hasVacationBalance = await _context.VacationBalances
                .AnyAsync(vb => vb.EmployeeId == employee.Id && vb.PeriodStart == hireDate);

            if (!hasVacationBalance)
            {
                var vacationBalance = new VacationBalance
                {
                    EmployeeId = employee.Id,
                    TotalDaysPerYear = 24,   // standard for regular employees
                    UsedDays = 0,
                    ByExperienceBonusDays = 0,
                    Year = hireDate.Year,
                    PeriodStart = hireDate,
                    PeriodEnd = nextYearEnd
                };

                _context.VacationBalances.Add(vacationBalance);
                await _context.SaveChangesAsync();
            }


            await _context.SaveChangesAsync();
        }

    }
}
