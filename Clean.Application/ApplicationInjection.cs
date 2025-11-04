using Clean.Application.Abstractions;
using Clean.Application.Services.Department;
using Clean.Application.Services.Employee;
using Clean.Application.Services.JWT;
using Clean.Application.Services.PayrollRecord;
using Clean.Application.Services.SalaryAnomaly;
using Clean.Application.Services.SalaryHistory;
using Clean.Application.Services.User;
using Clean.Application.Services.VacationBalance;
using Clean.Application.Services.VacationRecord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clean.Application;

public static class ApplicationInjection
{

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Service Injections
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IEmployeeService, EmployeeService>();
        services.AddTransient<IDepartmentService, DepartmentService>();
        services.AddTransient<IVacationBalanceService, VacationBalanceService>();
        services.AddTransient<IVacationRecordService, VacationRecordService>();
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.AddTransient<ISalaryHistoryService, SalaryHistoryService>();
        services.AddTransient<IPayrollRecordService, PayrollRecordService>();
        services.AddTransient<ISalaryAnomalyService, SalaryAnomalyService>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<JwtTokenService>(configuration.GetSection(JwtOptions.SectionName));
   
        return services;
    }
}