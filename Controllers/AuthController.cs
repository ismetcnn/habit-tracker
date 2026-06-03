using HabitTracker.DTOs;
using HabitTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitTracker.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, message, data) = await _authService.RegisterAsync(request);
        if (!success)
            return BadRequest(new { message });

        return Ok(data);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, message, data) = await _authService.LoginAsync(request);
        if (!success)
            return Unauthorized(new { message });

        return Ok(data);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var (success, message, data) = await _authService.RefreshAsync(request);
        if (!success)
            return Unauthorized(new { message });

        return Ok(data);
    }
}
