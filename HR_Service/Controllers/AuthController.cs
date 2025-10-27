using Clean.Application.Abstractions;
using Clean.Application.Dtos.Users;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("register")]
    [PermissionAuthorize(PermissionConstants.User.ManageEmployees)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterUserDto dto)
    {
        var response = await _userService.RegisterUserAsync(dto);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto dto)
    {
        var response = await _userService.LoginUserAsync(dto);
        return Ok(response);
    }
}