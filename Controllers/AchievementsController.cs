using System.Security.Claims;
using HabitTracker.DTOs;
using HabitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitTracker.Controllers;

[ApiController]
[Route("api/achievements")]
[Authorize]
[EnableRateLimiting("api")]
public class AchievementsController : ControllerBase
{
    private readonly AchievementService _achievementService;

    public AchievementsController(AchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (_, message, data) = await _achievementService.GetUserAchievementsAsync(GetUserId());
        return Ok(ApiResponse<List<AchievementResponse>>.Ok(data!, message));
    }
}
