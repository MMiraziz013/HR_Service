using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clean.Application.Services.Permission;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Clean.Application.Services.JWT;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly IConfiguration _configuration;

    public JwtTokenService(
        IOptions<JwtOptions> options,
        UserManager<Domain.Entities.User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        IConfiguration configuration)
    {
        _options = options.Value;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<string> CreateTokenAccessAsync(Domain.Entities.User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? string.Empty;

        // Start with basic user info
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Role, roleName)
        };

        // Fetch permissions assigned to the role
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role != null)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            claims.AddRange(roleClaims.Where(c => c.Type == "Permissions"));
        }

        // Build the JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public async Task<string> GenerateJwtToken(Domain.Entities.User user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var roles = await _userManager.GetRolesAsync(user);

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var permissions = RolePermissionService.GetPermissionsByRoles(roles);
        claims.AddRange(permissions.Select(p=>  new Claim("Permission", p)));

        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );


        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }

}



