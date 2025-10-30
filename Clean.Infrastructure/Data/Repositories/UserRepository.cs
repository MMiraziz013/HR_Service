using System.Net;
using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
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
            .ThenInclude(e => e!.Department)
            .Include(u=> u.Employee)
            .ThenInclude(e=> e!.SalaryHistories)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    // 🔍 Now supports optional search
        public async Task<List<User>> GetUsersAsync(string? search = null)
        {
            var query = _userManager.Users
                .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
                .Include(u=> u.Employee)
                .ThenInclude(e=> e!.SalaryHistories)
                .Where(u => u.Role != UserRole.Admin)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    EF.Functions.ILike(u.UserName!, $"%{search}%") ||
                    EF.Functions.ILike(u.Email!, $"%{search}%") ||
                    EF.Functions.ILike(u.PhoneNumber!, $"%{search}%"));
            }

            return await query.OrderBy(u => u.Id).ToListAsync();
        }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _userManager.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email!, $"%{email}%"));
    }

    public async Task<IdentityResult> AddAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> UpdatePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var isUpdated = await _userManager.ChangePasswordAsync(user!, currentPassword, newPassword);
        return isUpdated;
    }

    public async Task<bool> UpdateAsync(User user)
    {
        await _userManager.UpdateNormalizedEmailAsync(user);
        await _userManager.UpdateNormalizedUserNameAsync(user);
        _context.Update(user);
        var isUpdated = await _context.SaveChangesAsync();
        return isUpdated > 0;
    }

    public async Task DeleteAsync(User user)
    {
        await _userManager.DeleteAsync(user);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _userManager.Users.AnyAsync(u => EF.Functions.ILike(u.Email!, email));
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _userManager.Users.AnyAsync(u => EF.Functions.ILike(u.UserName!, username));
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users.AnyAsync(u => EF.Functions.ILike(u.PhoneNumber!, phoneNumber));
    }
}
