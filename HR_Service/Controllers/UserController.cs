using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Users;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace HR_Service.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get information about the currently logged-in user.
    /// </summary>
    [HttpGet("me")]
    [PermissionAuthorize(PermissionConstants.User.ManageSelf)]
    public async Task<IActionResult> GetMyInfoAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.GetUserProfileAsync(userId);
        return Ok(response);
    }

    /// <summary>
    /// Get all user profiles (Admin/HR only).
    /// </summary>
    [HttpGet]
    [PermissionAuthorize(PermissionConstants.User.ManageEmployees)]
    public async Task<IActionResult> GetAllAsync([FromQuery] string? search = null)
    {
        var response = await _userService.GetAllUserProfilesAsync(search);
        return Ok(response);
    }

    /// <summary>
    /// Update the current user's profile.
    /// </summary>
    [HttpPut("me")]
    [PermissionAuthorize(PermissionConstants.User.ManageSelf)]
    public async Task<IActionResult> UpdateMyProfileAsync([FromBody] UpdateUserProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.UpdateMyProfileAsync(dto, userId);
        return Ok(response);
    }

    /// <summary>
    /// Update the current user's password.
    /// </summary>
    [HttpPut("me/password")]
    [PermissionAuthorize(PermissionConstants.User.ManageSelf)]
    public async Task<IActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.UpdatePasswordAsync(dto, userId);
        return Ok(response);
    }
}
