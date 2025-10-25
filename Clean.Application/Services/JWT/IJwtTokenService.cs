using Clean.Domain.Entities;

namespace Clean.Application.Services.JWT;

public interface IJwtTokenService
{
    public Task<string> CreateTokenAccessAsync(Domain.Entities.User user);
    public Task<string> GenerateJwtToken(Domain.Entities.User user);
}