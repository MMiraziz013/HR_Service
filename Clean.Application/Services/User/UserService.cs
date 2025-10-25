using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Users;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly UserManager<User> _userManager;
    private readonly IDataContext _context;

    public UserService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        UserManager<User> userManager,
        IDataContext context)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _userManager = userManager;
        _context = context;
    }

    public async Task<Response<string>> RegisterUserAsync(RegisterUserDto dto)
    {
        // ✅ Check if email already exists
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
        {
            return new Response<string>(HttpStatusCode.BadRequest, message: "A user with this email already exists.");
        }

        // ✅ Check if username already exists
        if (await _userRepository.ExistsByUsernameAsync(dto.Username))
        {
            return new Response<string>(HttpStatusCode.BadRequest, message: "A user with this username already exists.");
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

            // ✅ Create Employee entity linked to the user
            var employee = new Employee
            {
                FirstName = dto.FirstName, // can be improved later when you add names
                LastName = dto.LastName,
                Position = dto.Position,
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                BaseSalary = dto.BaseSalary,
                IsActive = true,
                DepartmentId = dto.DepartmentId, // placeholder (to be validated/selected from dto later)
                UserId = user.Id
            };

            var added = await _employeeRepository.AddAsync(employee);
            if (!added)
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

    public Task<Response<object>> LoginUserAsync(LoginDto login)
    {
        throw new NotImplementedException();
    }

    public Task<Response<UserProfileDto>> GetUserProfileAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> UpdatePasswordAsync(UpdatePasswordDto dto, string userId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> UpdateMyProfileAsync(UpdateUserProfileDto update, string userId)
    {
        throw new NotImplementedException();
    }
}
