using System.Security.Claims;
using HabitTracker.DTOs;
using HabitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitTracker.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
[EnableRateLimiting("api")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;

    public ProfileController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _profileService.GetProfileAsync(GetUserId());
        if (profile is null)
            return NotFound(ApiResponse<ProfileResponse>.Fail("User not found."));

        return Ok(ApiResponse<ProfileResponse>.Ok(profile));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var (success, message, data) = await _profileService.UpdateProfileAsync(GetUserId(), request);
        if (!success)
            return BadRequest(ApiResponse<ProfileResponse>.Fail(message));

        return Ok(ApiResponse<ProfileResponse>.Ok(data!, message));
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var (success, message) = await _profileService.ChangePasswordAsync(GetUserId(), request);
        if (!success)
            return BadRequest(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var (success, message) = await _profileService.DeleteAccountAsync(GetUserId(), request.Password);
        if (!success)
            return BadRequest(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }
}
