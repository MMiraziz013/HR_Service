// using Clean.Application.Abstractions;
// using Clean.Domain.Entities;
// using Clean.Domain.Enums;
// using Microsoft.AspNetCore.Identity;
//
// namespace Clean.Infrastructure.Data.Seed;
//
// public class SeedHRUsers : IDataSeeder
// {
//     private readonly UserManager<User> _userManager;
//     private readonly RoleManager<IdentityRole<int>> _roleManager;
//
//     public SeedHRUsers(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
//     {
//         _userManager = userManager;
//         _roleManager = roleManager;
//     }
//
//     public async Task SeedAsync()
//     {
//         const string hrRole = "HR Manager";
//
//         if (!await _roleManager.RoleExistsAsync(hrRole))
//             await _roleManager.CreateAsync(new IdentityRole<int>(hrRole));
//
//         var hrUsers = new List<(string Email, string FullName)>
//         {
//             ("hr1@company.com", "HR Manager 1"),
//             ("hr2@company.com", "HR Specialist 2")
//         };
//
//         foreach (var (email, fullName) in hrUsers)
//         {
//             var user = await _userManager.FindByEmailAsync(email);
//             if (user == null)
//             {
//                 user = new User
//                 {
//                     UserName = email,
//                     Email = email,
//                     EmailConfirmed = true,
//                     Role = UserRole.HrManager
//                 };
//
//                 var result = await _userManager.CreateAsync(user, "Hr@12345");
//                 if (result.Succeeded)
//                     await _userManager.AddToRoleAsync(user, hrRole);
//             }
//         }
//     }
// }
