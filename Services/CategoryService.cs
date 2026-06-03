using HabitTracker.Data;
using HabitTracker.DTOs;
using HabitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Services;

public class CategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message, List<CategoryResponse>? Data)> GetUserCategoriesAsync(Guid userId)
    {
        var categories = await _db.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                Icon = c.Icon,
                CreatedAt = c.CreatedAt,
                HabitCount = c.Habits.Count(h => h.IsActive)
            })
            .ToListAsync();

        return (true, string.Empty, categories);
    }

    public async Task<(bool Success, string Message, CategoryResponse? Data)> CreateAsync(Guid userId, CreateCategoryRequest request)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            Icon = request.Icon,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return (true, "Category created.", new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            Icon = category.Icon,
            CreatedAt = category.CreatedAt,
            HabitCount = 0
        });
    }

    public async Task<(bool Success, string Message, CategoryResponse? Data)> UpdateAsync(Guid userId, Guid categoryId, UpdateCategoryRequest request)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category is null)
            return (false, "Category not found.", null);

        category.Name = request.Name;
        category.Color = request.Color;
        category.Icon = request.Icon;

        await _db.SaveChangesAsync();

        var habitCount = await _db.Habits
            .CountAsync(h => h.CategoryId == categoryId && h.IsActive);

        return (true, "Category updated.", new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            Icon = category.Icon,
            CreatedAt = category.CreatedAt,
            HabitCount = habitCount
        });
    }

    public async Task<(bool Success, string Message, object? Data)> DeleteAsync(Guid userId, Guid categoryId)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category is null)
            return (false, "Category not found.", null);

        await _db.Habits
            .Where(h => h.CategoryId == categoryId)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.CategoryId, (Guid?)null));

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return (true, "Category deleted.", null);
    }
}
