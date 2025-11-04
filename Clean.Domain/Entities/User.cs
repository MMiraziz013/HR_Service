using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Clean.Domain.Entities;

public class User : IdentityUser<int>
{
    public DateTime RegistrationDate { get; set; }
    public UserRole Role { get; set; }
    
    public Employee? Employee { get; set; }

    public int? EmployeeId { get; set; }
}