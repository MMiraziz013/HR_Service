using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Users;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterUserDto register)
    {
        var response = await _userService.RegisterUserAsync(register);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUserAsync([FromBody] LoginDto dto)
    {
        var response = await _userService.LoginUserAsync(dto);
        return Ok(response);
    }
    
    [HttpGet("get-user-info")]
    [PermissionAuthorize(PermissionConstants.User.ManageSelf)]
    public async Task<IActionResult> GetUserInfoAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.GetUserProfileAsync(userId);
        return Ok(response);
    }

    [HttpGet("get-all-users")]
    [PermissionAuthorize(PermissionConstants.User.ManageEmployees)]
    public async Task<IActionResult> GetAllUserProfiles()
    {
        var response = await _userService.GetAllUserProfilesAsync();
        return Ok(response);
    }

    [HttpPut("update-password")]
    [PermissionAuthorize(PermissionConstants.User.ManageSelf)]
    public async Task<IActionResult> UpdatePasswordAsync(UpdatePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.UpdatePasswordAsync(dto, userId);
        return Ok(response);
    }

    [HttpPut("update-profile")]
    [PermissionAuthorize(PermissionConstants.User.ManageSelf)]
    public async Task<IActionResult> UpdateMyProfileAsync(UpdateUserProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.UpdateMyProfileAsync(dto, userId);
        return Ok(response);
    }
}