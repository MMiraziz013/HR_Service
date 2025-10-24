using Clean.Domain.Entities;

namespace Clean.Application.Services.JWT;

public interface IJwtTokenService
{
    public Task<string> CreateTokenAccessAsync(User user);
    public Task<string> GenerateJwtToken(User user);
}