using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Users;

public class UpdateUserProfileDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}