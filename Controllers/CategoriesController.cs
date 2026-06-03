using System.Security.Claims;
using HabitTracker.DTOs;
using HabitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitTracker.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
[EnableRateLimiting("api")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (_, message, data) = await _categoryService.GetUserCategoriesAsync(GetUserId());
        return Ok(ApiResponse<List<CategoryResponse>>.Ok(data!, message));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var (_, message, data) = await _categoryService.CreateAsync(GetUserId(), request);
        return Ok(ApiResponse<CategoryResponse>.Ok(data!, message));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var (success, message, data) = await _categoryService.UpdateAsync(GetUserId(), id, request);
        if (!success)
            return NotFound(ApiResponse<CategoryResponse>.Fail(message));

        return Ok(ApiResponse<CategoryResponse>.Ok(data!, message));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, message, _) = await _categoryService.DeleteAsync(GetUserId(), id);
        if (!success)
            return NotFound(ApiResponse<object?>.Fail(message));

        return Ok(ApiResponse<object?>.Ok(null, message));
    }
}
