using HabitTracker.DTOs;
using HabitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitTracker.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[EnableRateLimiting("admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(ApiResponse<AdminDashboardStats>.Ok(stats));
    }

    [HttpGet("completions/weekly")]
    public async Task<IActionResult> GetWeeklyCompletions()
    {
        var stats = await _adminService.GetLast7DaysCompletionsAsync();
        return Ok(ApiResponse<List<DailyCompletionStat>>.Ok(stats));
    }

    [HttpGet("habits/top")]
    public async Task<IActionResult> GetTopHabits([FromQuery] int count = 5)
    {
        var stats = await _adminService.GetTopHabitsAsync(count);
        return Ok(ApiResponse<List<TopHabitStat>>.Ok(stats));
    }

    [HttpGet("achievements/recent")]
    public async Task<IActionResult> GetRecentAchievements([FromQuery] int count = 10)
    {
        var stats = await _adminService.GetRecentAchievementsAsync(count);
        return Ok(ApiResponse<List<RecentAchievementStat>>.Ok(stats));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? search, [FromQuery] int page = 1)
    {
        var result = await _adminService.GetAllUsersAsync(search, page);
        return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(result));
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserDetail(Guid id)
    {
        var detail = await _adminService.GetUserDetailAsync(id);
        if (detail is null)
            return NotFound(ApiResponse<AdminUserDetail>.Fail("User not found."));

        return Ok(ApiResponse<AdminUserDetail>.Ok(detail));
    }

    [HttpGet("habits")]
    public async Task<IActionResult> GetAllHabits([FromQuery] string? search, [FromQuery] string? category, [FromQuery] int page = 1)
    {
        var result = await _adminService.GetAllHabitsAsync(search, category, page);
        return Ok(ApiResponse<PagedResult<AdminHabitDto>>.Ok(result));
    }

    [HttpPost("users/{id:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid id)
    {
        var (success, message) = await _adminService.BanUserAsync(id);
        if (!success)
            return NotFound(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }

    [HttpPost("users/{id:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid id)
    {
        var (success, message) = await _adminService.UnbanUserAsync(id);
        if (!success)
            return NotFound(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }
}
