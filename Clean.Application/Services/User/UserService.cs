using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Employee;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Users;
using Clean.Application.Services.Enum;
using Clean.Application.Services.JWT;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.User;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IVacationBalanceRepository _vacationBalanceRepository;
    private readonly ISalaryHistoryRepository _salaryHistoryRepository;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UserService> _logger;
    private readonly IDataContext _context;

    public UserService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IVacationBalanceRepository vacationBalanceRepository,
        ISalaryHistoryRepository salaryHistoryRepository,
        UserManager<Domain.Entities.User> userManager,
        IJwtTokenService tokenService,
        IConfiguration configuration,
        ICacheService cacheService,
        ILogger<UserService> logger,
        IDataContext context)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _vacationBalanceRepository = vacationBalanceRepository;
        _salaryHistoryRepository = salaryHistoryRepository;
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _cacheService = cacheService;
        _logger = logger;
        _context = context;
    }

    public async Task<Response<string>> RegisterUserAsync(RegisterUserDto dto)
    {
        try
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

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new Domain.Entities.User
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

                await _userManager.AddToRoleAsync(user, dto.UserRole.GetDisplayName());
                
                var employee = new Domain.Entities.Employee
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Position = dto.Position,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
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

                user.EmployeeId = employee.Id;
                var updatedUser = await _userManager.UpdateAsync(user);

                if (!updatedUser.Succeeded) // Check if updating the user failed
                {
                    await transaction.RollbackAsync();
                    var identityErrors = string.Join(" | ", updatedUser.Errors.Select(e => e.Description));
                    var fullMessage = $"Employee created but failed to update user link. Rolled back. Identity Errors: {identityErrors}";
                
                    return new Response<string>(HttpStatusCode.InternalServerError, message: fullMessage);
                }
                
                var vacationBalance = new Domain.Entities.VacationBalance
                {
                    TotalDaysPerYear = 24,
                    UsedDays = 0,
                    Year = DateTime.Today.Year,
                    ByExperienceBonusDays = 0,
                    PeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
                    PeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1).AddDays(-1)),
                    EmployeeId = employee.Id,
                };

                var balanceExists =
                    await _vacationBalanceRepository.ExistsAsync(vacationBalance.EmployeeId, vacationBalance.Year);
                
                if (balanceExists)
                {
                    await transaction.RollbackAsync();
                    return new Response<string>(HttpStatusCode.InternalServerError, message: "This employee already has vacation balance.");

                }
                var isBalanceAdded = await _vacationBalanceRepository.AddVacationBalanceAsync(vacationBalance);

                
                if (isBalanceAdded == false)
                {
                    await transaction.RollbackAsync();
                    return new Response<string>(HttpStatusCode.InternalServerError, message: "Failed to create vacation balance for the employee.");
                }

                var salaryHistory = new Domain.Entities.SalaryHistory
                {
                    Month = DateOnly.FromDateTime(DateTime.Today),
                    BaseAmount = dto.BaseSalary,
                    BonusAmount = 0,
                    EmployeeId = employee.Id,
                };

                var isSalaryAdded = await _salaryHistoryRepository.AddAsync(salaryHistory);
                if (isSalaryAdded == false)
                {
                    await transaction.RollbackAsync();
                    return new Response<string>(HttpStatusCode.InternalServerError, message: "Failed to salary history for the employee.");
                }

                await transaction.CommitAsync();
                await _cacheService.RemoveAsync("all_users_all");
                
                await _cacheService.RemoveByPatternAsync("employees_*");
                
                await _cacheService.RemoveAsync($"department_with_employees_{employee.DepartmentId}");
                
                await _cacheService.RemoveByPatternAsync("departments_summary_*");
        
                await _cacheService.RemoveByPatternAsync("departments_with_employees_*");
                await _cacheService.RemoveAsync($"user_profile_{user.Id}"); // Invalidate single user profile cache

                //TODO: Add more cache invalidations
                
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during RegisterUserAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, $"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<Response<object>> LoginUserAsync(LoginDto login)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(login.Username);
            if (user is null)
                return new Response<object>(HttpStatusCode.BadRequest, "No user with this username.");

            if (!await _userManager.CheckPasswordAsync(user, login.Password))
                return new Response<object>(HttpStatusCode.BadRequest, "Incorrect password.");

            var jwtToken = await _tokenService.GenerateJwtToken(user);
            return new Response<object>(HttpStatusCode.OK, "Login Successful", new
            {
                Token = jwtToken,
                ExpiresAt = DateTime.Now.AddMinutes(double.Parse(_configuration["JWT:AccessTokenMinutes"]!)).ToString("g")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LoginUserAsync for {Username}", login.Username);
            return new Response<object>(HttpStatusCode.InternalServerError, "Login failed due to an unexpected error.");
        }
    }

    public async Task<Response<UserProfileDto>> GetUserProfileAsync(int userId)
    {
        try
        {
            var cacheKey = $"user_profile_{userId}";
            var cached = await _cacheService.GetAsync<UserProfileDto>(cacheKey);
            if (cached != null)
                return new Response<UserProfileDto>(HttpStatusCode.OK, "Profile Retrieved!", cached);

            var employee = await _userRepository.GetByIdAsync(userId);
            if (employee is null)
                return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "No such user in the system.");

            if (employee.Employee is null)
                return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This user is not an employee!");

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
                    BaseSalary = employee.Employee.SalaryHistories
                        .OrderByDescending(sh => sh.Month)
                        .Select(sh => sh.BaseAmount)
                        .FirstOrDefault(),
                    DepartmentName = employee.Employee.Department.Name,
                    IsActive = employee.Employee.IsActive,
                    Position = employee.Employee.Position,
                    HireDate = employee.Employee.HireDate.ToString("yyyy-MM-dd")
                }
            };

            await _cacheService.SetAsync(cacheKey, profileDto, TimeSpan.FromMinutes(10));
            return new Response<UserProfileDto>(HttpStatusCode.OK, "Profile Retrieved!", profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for ID {UserId}", userId);
            return new Response<UserProfileDto>(HttpStatusCode.InternalServerError, "Failed to retrieve profile due to an unexpected error.");
        }
    }
    public async Task<Response<List<UserProfileDto>>> GetAllUserProfilesAsync(string? search = null)
    {
        try
        {
            var cacheKey = $"all_users_{search ?? "all"}";
            var cached = await _cacheService.GetAsync<List<UserProfileDto>>(cacheKey);
            if (cached != null)
                return new Response<List<UserProfileDto>>(HttpStatusCode.OK, "Users retrieved successfully!", cached);

            var users = await _userRepository.GetUsersAsync(search);
            if (users.Count == 0)
                return new Response<List<UserProfileDto>>(HttpStatusCode.NotFound, "No users found.");

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
                    BaseSalary = u.Employee.SalaryHistories
                        .OrderByDescending(sh => sh.Month)
                        .Select(sh => sh.BaseAmount)
                        .FirstOrDefault(),
                    DepartmentName = u.Employee.Department.Name,
                    IsActive = u.Employee.IsActive,
                    Position = u.Employee.Position,
                    HireDate = u.Employee.HireDate.ToString("yyyy-MM-dd")
                }
            }).ToList();

            await _cacheService.SetAsync(cacheKey, userProfiles, TimeSpan.FromMinutes(10));
            return new Response<List<UserProfileDto>>(HttpStatusCode.OK, "Users retrieved successfully!", userProfiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profiles with search {Search}", search);
            return new Response<List<UserProfileDto>>(HttpStatusCode.InternalServerError, "Failed to retrieve user profiles.");
        }
    }


    public async Task<Response<string>> UpdatePasswordAsync(UpdatePasswordDto dto, int userId)
    {
        try
        {
            var isUpdated = await _userRepository.UpdatePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            if (isUpdated.Succeeded)
                return new Response<string>(HttpStatusCode.OK, "Password Updated!");

            return new Response<string>(HttpStatusCode.BadRequest, isUpdated.Errors.Select(e => e.Description).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user {UserId}", userId);
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to update password.");
        }
    }

    public async Task<Response<UserProfileDto>> UpdateProfileAsync(UpdateUserProfileDto update)
    {
        try
        {
            var userToUpdate = await _userRepository.GetByEmployeeIdAsync(update.EmployeeId);
            if (userToUpdate is null)
                return new Response<UserProfileDto>(HttpStatusCode.NotFound, "User not found!");

            if (!string.IsNullOrWhiteSpace(update.Email) && update.Email != userToUpdate.Email)
            {
                if (await _userRepository.ExistsByEmailAsync(update.Email))
                    return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This email is already in the system!");

                userToUpdate.Email = update.Email;
            }

            if (!string.IsNullOrWhiteSpace(update.PhoneNumber) && update.PhoneNumber != userToUpdate.PhoneNumber)
            {
                if (await _userRepository.ExistsByPhoneNumberAsync(update.PhoneNumber))
                    return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This phone number is already in the system!");

                userToUpdate.PhoneNumber = update.PhoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(update.Username) && update.Username != userToUpdate.UserName)
            {
                if (await _userRepository.ExistsByUsernameAsync(update.Username))
                    return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "This username is already in the system!");

                userToUpdate.UserName = update.Username;
            }

            var isUpdated = await _userRepository.UpdateAsync(userToUpdate);
            if (isUpdated)
            {
                await _cacheService.RemoveAsync($"user_profile_{userToUpdate.Id}");
                await _cacheService.RemoveAsync("all_users_all");

                var updatedUser = new UserProfileDto
                {
                    Username = userToUpdate.UserName,
                    Email = userToUpdate.Email,
                    PhoneNumber = userToUpdate.PhoneNumber,
                    RegistrationDate = userToUpdate.RegistrationDate.ToString("yyyy-MM-dd"),
                    Role = userToUpdate.Role,
                    EmployeeInfo = new GetEmployeeDto
                    {
                        Id = userToUpdate.Employee!.Id,
                        FirstName = userToUpdate.Employee.FirstName,
                        LastName = userToUpdate.Employee.LastName,
                        BaseSalary = userToUpdate.Employee.SalaryHistories
                            .OrderByDescending(sh => sh.Month)
                            .Select(sh => sh.BaseAmount)
                            .FirstOrDefault(),
                        DepartmentName = userToUpdate.Employee.Department.Name,
                        IsActive = userToUpdate.Employee.IsActive,
                        Position = userToUpdate.Employee.Position,
                        HireDate = userToUpdate.Employee.HireDate.ToString("yyyy-MM-dd")
                    }
                };

                return new Response<UserProfileDto>(HttpStatusCode.OK, "Profile updated successfully!", updatedUser);
            }

            return new Response<UserProfileDto>(HttpStatusCode.BadRequest, "Couldn't update the profile");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for EmployeeId {EmployeeId}", update.EmployeeId);
            return new Response<UserProfileDto>(HttpStatusCode.InternalServerError, "Failed to update profile.");
        }
    }

}