using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Users;

public class UserProfileDto
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public UserRole Role { get; set; }
    public string RegistrationDate { get; set; }
}