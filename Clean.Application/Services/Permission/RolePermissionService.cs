using Clean.Application.Security.Permission;

namespace Clean.Application.Services.Permission;

public static class RolePermissionService
{
    private static readonly Dictionary<string, List<string>> _rolePermissions = new()
    {
        {
            RoleConstants.Admin, new List<string>
            {
                PermissionConstants.Employees.ManageAll,
                PermissionConstants.Employees.View,
                PermissionConstants.Employees.Manage,
                
                PermissionConstants.SalaryHistories.View,
                PermissionConstants.SalaryHistories.Manage,
                
                PermissionConstants.VacationRecords.View,
                PermissionConstants.VacationRecords.Manage,
                
                PermissionConstants.VacationBalance.View,
                PermissionConstants.VacationBalance.Manage,
                
                PermissionConstants.SalaryAnomalies.View,
                PermissionConstants.SalaryAnomalies.Manage,
                
                PermissionConstants.Departments.View,
                PermissionConstants.Departments.Manage,
                
                PermissionConstants.PayrollRecords.View,
                PermissionConstants.PayrollRecords.Manage,
                
                PermissionConstants.User.ManageAll,
                PermissionConstants.User.ManageSelf,
                PermissionConstants.User.ManageEmployees,
            }
        },
        {
            RoleConstants.HrManager, new List<string>
            {
                PermissionConstants.Employees.View,
                PermissionConstants.Employees.Manage,
                
                PermissionConstants.SalaryHistories.View,
                PermissionConstants.SalaryHistories.Manage,
                
                PermissionConstants.VacationRecords.View,
                PermissionConstants.VacationRecords.Manage,
                
                PermissionConstants.VacationBalance.View,
                PermissionConstants.VacationBalance.Manage,
                
                PermissionConstants.SalaryAnomalies.View,
                PermissionConstants.SalaryAnomalies.Manage,
                
                PermissionConstants.Departments.View,
                PermissionConstants.Departments.Manage,
                
                PermissionConstants.PayrollRecords.View,
                PermissionConstants.PayrollRecords.Manage,
                
                PermissionConstants.User.ManageEmployees,
                PermissionConstants.User.ManageSelf
            }
        },
        {
            RoleConstants.Employee, new List<string>
            {
                PermissionConstants.User.ManageSelf,
                PermissionConstants.Employees.View,
                
                //TODO: Check later if employees should see their payment calculations
                // PermissionConstants.SalaryHistories.View,
                
                PermissionConstants.VacationRecords.View,
                PermissionConstants.VacationBalance.View,
                PermissionConstants.Departments.View,
                PermissionConstants.PayrollRecords.View,

            }
        }
    };

    public static IEnumerable<string> GetPermissionsByRoles(IEnumerable<string> roles)
    {
        return roles
            .SelectMany(role => _rolePermissions.TryGetValue(role, out var permissions)
                ? permissions
                : Enumerable.Empty<string>())
            .Distinct()
            .ToList();
    }
    
    public static IEnumerable<string> GetAllRoles()
    {
        return _rolePermissions.Keys;
    }
}
