using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clean.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;

    public UserRepository(DataContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _userManager.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<IdentityResult> AddAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        await _userManager.DeleteAsync(user);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email) != null;
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _userManager.FindByNameAsync(username) != null;
    }
}