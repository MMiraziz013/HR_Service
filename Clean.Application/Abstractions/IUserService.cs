using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Users;

namespace Clean.Application.Abstractions;

public interface IUserService
{
    Task<Response<string>> RegisterUserAsync(RegisterUserDto register);

    Task<Response<object>> LoginUserAsync(LoginDto login);
    
    Task<Response<UserProfileDto>> GetUserProfileAsync(int userId);
    
    Task<Response<List<UserProfileDto>>> GetAllUserProfilesAsync();
    
    Task<Response<string>> UpdatePasswordAsync(UpdatePasswordDto dto, int userId);

    Task<Response<UserProfileDto>> UpdateMyProfileAsync(UpdateUserProfileDto update, int userId);
}