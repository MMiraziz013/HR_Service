using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Users;
using Clean.Application.Services.Enum;
using Clean.Application.Services.JWT;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDataContext _context;

    public UserService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        UserManager<User> userManager,
        IJwtTokenService tokenService,
        IConfiguration configuration,
        IDataContext context)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _context = context;
    }

    public async Task<Response<string>> RegisterUserAsync(RegisterUserDto dto)
    {
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
        {
            return new Response<string>(HttpStatusCode.BadRequest, message: "A user with this email already exists.");
        }

        if (await _userRepository.ExistsByUsernameAsync(dto.Username))
        {
            return new Response<string>(HttpStatusCode.BadRequest, message: "A user with this username already exists.");
        }

        var department = await _departmentRepository.GetDepartmentByIdAsync(dto.DepartmentId);
        if (department is null)
        {
            return new Response<string>(HttpStatusCode.BadRequest, message: "No such department to add employee to");
        }

        // ✅ Transaction begins
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.Phone,
                RegistrationDate = DateTime.UtcNow,
                Role = dto.UserRole,
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);

            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                var errors = createResult.Errors.Select(e => e.Description).ToList();
                return new Response<string>(HttpStatusCode.BadRequest, errors);
            }

            // This adds a record in the AspNetUserRoles table, that joins user and their roles.
            await _userManager.AddToRoleAsync(user, dto.UserRole.GetDisplayName());
            
            var employee = new Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Position = dto.Position,
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                BaseSalary = dto.BaseSalary,
                IsActive = true,
                DepartmentId = dto.DepartmentId,
                UserId = user.Id
            };

            var isAdded = await _employeeRepository.AddAsync(employee);
            if (isAdded == false)
            {
                await transaction.RollbackAsync();
                return new Response<string>(HttpStatusCode.InternalServerError, message: "Failed to create employee record.");
            }

            await transaction.CommitAsync();

            return new Response<string>(
                HttpStatusCode.OK,
                message: "User and employee created successfully.",
                data: user.Id.ToString()
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new Response<string>(HttpStatusCode.InternalServerError, message: $"An error occurred: {ex.Message}");
        }
    }

    public async Task<Response<object>> LoginUserAsync(LoginDto login)
    {
        var user = await _userManager.FindByNameAsync(login.Username);
        if (user is null)
        {
            return new Response<object>(HttpStatusCode.BadRequest, "No user with this username");
        }

        var correctPassword = await _userManager.CheckPasswordAsync(user, login.Password);
        if (correctPassword == false)
        {
            return new Response<object>(HttpStatusCode.BadRequest, "Incorrect password");
        }
        
        var jwtToken = await _tokenService.GenerateJwtToken(user);
        return new Response<object>(HttpStatusCode.OK, "Login Successful", new
        {
            Token = jwtToken,
            ExpiresAt = DateTime.Now.AddMinutes(double.Parse(_configuration["JWT:AccessTokenMinutes"]!))
                .ToString("g")
        });
    }

    public async Task<Response<UserProfileDto>> GetUserProfileAsync(int userId)
    {
        var employee = await _userRepository.GetByIdAsync(userId);
        if (employee is null)
        {
            return new Response<UserProfileDto>(HttpStatusCode.BadRequest, message: "No such user in the system");
        }

        if (employee.Employee is null)
        {
            return new Response<UserProfileDto>(HttpStatusCode.BadRequest, message: "This user is not an employee!");
        }

        var profileDto = new UserProfileDto
        {
            Username = employee.UserName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            RegistrationDate = employee.RegistrationDate.ToString("yyyy-MM-dd"),
            Role = employee.Role,
            EmployeeInfo = new GetEmployeeDto
            {
                Id = employee.Employee.Id,
                FirstName = employee.Employee.FirstName,
                LastName = employee.Employee.LastName,
                BaseSalary = employee.Employee.BaseSalary,
                DepartmentName = employee.Employee.Department.Name,
                IsActive = employee.Employee.IsActive,
                Position = employee.Employee.Position,
                HireDate = employee.Employee.HireDate.ToString("yyyy-MM-dd")
            }
        };

        return new Response<UserProfileDto>(HttpStatusCode.OK, "Profile Retrieved!", profileDto);
    }

    public async Task<Response<List<UserProfileDto>>> GetAllUserProfilesAsync()
    {
        var users = await _userRepository.GetUsersAsync();
        var userProfiles = users.Select(u => new UserProfileDto
        {
            Username = u.UserName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            RegistrationDate = u.RegistrationDate.ToString("yyyy-MM-dd"),
            Role = u.Role,
            EmployeeInfo = new GetEmployeeDto
            {
                Id = u.Employee!.Id,
                FirstName = u.Employee.FirstName,
                LastName = u.Employee.LastName,
                BaseSalary = u.Employee.BaseSalary,
                DepartmentName = u.Employee.Department.Name,
                IsActive = u.Employee.IsActive,
                Position = u.Employee.Position,
                HireDate = u.Employee.HireDate.ToString("yyyy-MM-dd")
            }
        }).ToList();

        return new Response<List<UserProfileDto>>(HttpStatusCode.OK, userProfiles);
    }

    public async Task<Response<string>> UpdatePasswordAsync(UpdatePasswordDto dto, int userId)
    {
        var isUpdated = await _userRepository.UpdatePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        if (isUpdated.Succeeded)
        {
            return new Response<string>(HttpStatusCode.OK, message: "Password Updated!");
        }

        return new Response<string>(HttpStatusCode.BadRequest, isUpdated.Errors.Select(e=> e.Description).ToList());
    }

    public async Task<Response<UserProfileDto>> UpdateMyProfileAsync(UpdateUserProfileDto update, int userId)
    {
        var userToUpdate = await _userRepository.GetByIdAsync(userId);

        if (userToUpdate is null)
        {
            return new Response<UserProfileDto>(HttpStatusCode.NotFound, "User not found!");
        }

        if (!string.IsNullOrWhiteSpace(update.Email) && update.Email != userToUpdate.Email)
        {
            if (await _userRepository.ExistsByEmailAsync(update.Email))
            {
                return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This email is already in the system!");
            }

            userToUpdate.Email = update.Email;
        }

        if (!string.IsNullOrWhiteSpace(update.PhoneNumber) && update.PhoneNumber != userToUpdate.PhoneNumber)
        {
            if (await _userRepository.ExistsByPhoneNumberAsync(update.PhoneNumber))
            {
                return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This phone number is already in the system!");
            }

            userToUpdate.PhoneNumber = update.PhoneNumber;
        }

        if (!string.IsNullOrWhiteSpace(update.Username) && update.Username != userToUpdate.UserName)
        {
            if (await _userRepository.ExistsByUsernameAsync(update.Username))
            {
                return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This username is already in the system!");

            }

            userToUpdate.UserName = update.Username;
        }

        var isUpdated = await _userRepository.UpdateAsync(userToUpdate);

        if (isUpdated)
        {
            var updatedUser = await GetUserProfileAsync(userId);
            return new Response<UserProfileDto>(HttpStatusCode.OK, "Profile updated successfully!", updatedUser.Data);
        }

        return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "Couldn't update the profile");
    }

}
