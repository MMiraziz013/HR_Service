namespace Clean.Application.Dtos.Users;

public abstract class UpdateUserProfileDto
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
}