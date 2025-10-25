using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Clean.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<IdentityResult> AddAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);

    Task<bool> ExistsByUsernameAsync(string username);
}