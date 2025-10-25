using System.ComponentModel.DataAnnotations;
using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Users;

public class RegisterUserDto
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Phone { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = default!;

    [Required]
    public decimal BaseSalary { get; set; }

    [Required]
    public UserRole UserRole { get; set; }

    [Required]
    public EmployeePosition Position { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public string FirstName { get; set; } = default!;

    [Required]
    public string LastName { get; set; } = default!;
}