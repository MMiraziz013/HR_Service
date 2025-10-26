namespace Clean.Application.Dtos.Users;

public class UpdatePasswordDto
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}