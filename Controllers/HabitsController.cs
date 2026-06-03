using System.Security.Claims;
using HabitTracker.DTOs;
using HabitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitTracker.Controllers;

[ApiController]
[Route("api/habits")]
[Authorize]
[EnableRateLimiting("api")]
public class HabitsController : ControllerBase
{
    private readonly HabitService _habitService;

    public HabitsController(HabitService habitService)
    {
        _habitService = habitService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (success, message, data) = await _habitService.GetUserHabitsAsync(GetUserId());
        return Ok(ApiResponse<List<HabitResponse>>.Ok(data!, message));
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted()
    {
        var (_, message, data) = await _habitService.GetDeletedHabitsAsync(GetUserId());
        return Ok(ApiResponse<List<HabitResponse>>.Ok(data!, message));
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var (success, message) = await _habitService.RestoreHabitAsync(GetUserId(), id);
        if (!success)
            return NotFound(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var (_, message, data) = await _habitService.GetOverallSummaryAsync(GetUserId());
        return Ok(ApiResponse<OverallSummary>.Ok(data!, message));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHabitRequest request)
    {
        var (success, message, data) = await _habitService.CreateAsync(GetUserId(), request);
        return Ok(ApiResponse<HabitResponse>.Ok(data!, message));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHabitRequest request)
    {
        var (success, message, data) = await _habitService.UpdateAsync(GetUserId(), id, request);
        if (!success)
            return NotFound(ApiResponse<HabitResponse>.Fail(message));

        return Ok(ApiResponse<HabitResponse>.Ok(data!, message));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, message, _) = await _habitService.DeleteAsync(GetUserId(), id);
        if (!success)
            return NotFound(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }

    [HttpGet("{id:guid}/weekly")]
    public async Task<IActionResult> GetWeekly(Guid id)
    {
        var (success, message, data) = await _habitService.GetHabitWeeklySummaryAsync(GetUserId(), id);
        if (!success)
            return NotFound(ApiResponse<HabitWeeklySummary>.Fail(message));

        return Ok(ApiResponse<HabitWeeklySummary>.Ok(data!, message));
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<IActionResult> GetStats(Guid id)
    {
        var (success, message, data) = await _habitService.GetStatsAsync(GetUserId(), id);
        if (!success)
            return NotFound(ApiResponse<HabitStatsResponse>.Fail(message));

        return Ok(ApiResponse<HabitStatsResponse>.Ok(data!, message));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteHabitRequest request)
    {
        var (success, message, data) = await _habitService.CompleteAsync(GetUserId(), id, request);
        if (!success)
            return BadRequest(ApiResponse<CompleteHabitResponse>.Fail(message));

        return Ok(ApiResponse<CompleteHabitResponse>.Ok(data!, message));
    }
}
