using Clean.Application.Dtos.Employee;
using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Users;

public class UserProfileDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public string? RegistrationDate { get; set; }

    public GetEmployeeDto? EmployeeInfo { get; set; }
}