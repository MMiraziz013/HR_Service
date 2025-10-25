using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Users;

namespace Clean.Application.Abstractions;

public interface IUserService
{
    public Task<Response<string>> RegisterUserAsync(RegisterUserDto register);

    public Task<Response<object>> LoginUserAsync(LoginDto login);
    
    public Task<Response<UserProfileDto>> GetUserProfileAsync(string userId);
    
    public Task<Response<string>> UpdatePasswordAsync(UpdatePasswordDto dto, string userId);

    public Task<Response<string>> UpdateMyProfileAsync(UpdateUserProfileDto update, string userId);
}