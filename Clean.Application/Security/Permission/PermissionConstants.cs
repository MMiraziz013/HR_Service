namespace Clean.Application.Security.Permission
{
    public static class PermissionConstants 
    {
        public const string ClaimType = "Permission";

        public static class Employees
        {
            public const string View = "Permissions.Employees.View";
            public const string Manage = "Permissions.Employees.Manage";
            public const string ManageAll = "Permissions.Employees.ManageAll";
        }

        public static class SalaryHistories
        {
            public const string View = "Permissions.SalaryHistories.View";
            public const string Manage = "Permissions.SalaryHistories.Manage";
        }

        public static class VacationRecords
        {
            public const string View = "Permissions.VacationRecords.View";
            public const string Manage = "Permissions.VacationRecords.Manage";
        }

        public static class VacationBalance
        {
            public const string View = "Permissions.VacationBalance.View";
            public const string Manage = "Permissions.VacationBalance.Manage";
        }

        public static class Departments
        {
            public const string View = "Permissions.Departments.View";
            public const string Manage = "Permissions.Departments.Manage";
        }

        public static class PayrollRecords
        {
            public const string View = "Permissions.PayrollRecords.View";
            public const string Manage = "Permissions.PayrollRecords.Manage";
        }

        public static class SalaryAnomalies
        {
            public const string View = "Permissions.SalaryAnomalies.View";
            public const string Manage = "Permissions.SalaryAnomalies.Manage";
        }

        public static class User
        {
            public const string ManageAll = "Permissions.User.ManageAll";
            public const string ManageEmployees = "Permissions.User.ManageEmployees";
            public const string ManageSelf = "Permissions.User.ManageSelf";
        }
    }
}
