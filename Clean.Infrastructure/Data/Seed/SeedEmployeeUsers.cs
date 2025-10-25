// using Clean.Application.Abstractions;
// using Clean.Domain.Entities;
// using Clean.Domain.Enums;
// using Microsoft.AspNetCore.Identity;
//
// namespace Clean.Infrastructure.Data.Seed;
//
// public class SeedEmployeeUsers : IDataSeeder
// {
//     private readonly UserManager<User> _userManager;
//     private readonly RoleManager<IdentityRole<int>> _roleManager;
//
//     public SeedEmployeeUsers(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
//     {
//         _userManager = userManager;
//         _roleManager = roleManager;
//     }
//
//     public async Task SeedAsync()
//     {
//         const string employeeRole = "Employee";
//
//         if (!await _roleManager.RoleExistsAsync(employeeRole))
//             await _roleManager.CreateAsync(new IdentityRole<int>(employeeRole));
//
//         var employeeUsers = new List<(string Email, string FullName)>
//         {
//             ("john.doe@company.com", "John Doe"),
//             ("jane.smith@company.com", "Jane Smith")
//         };
//
//         foreach (var (email, fullName) in employeeUsers)
//         {
//             var user = await _userManager.FindByEmailAsync(email);
//             if (user == null)
//             {
//                 user = new User
//                 {
//                     UserName = fullName,
//                     Email = email,
//                     EmailConfirmed = true,
//                     Role = UserRole.Employee
//                 };
//
//                 var result = await _userManager.CreateAsync(user, "Emp@12345");
//                 if (result.Succeeded)
//                     await _userManager.AddToRoleAsync(user, employeeRole);
//             }
//         }
//     }
// }
