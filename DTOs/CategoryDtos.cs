using System.ComponentModel.DataAnnotations;

namespace HabitTracker.DTOs;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }
    public string? Icon { get; set; }
}

public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }
    public string? Icon { get; set; }
}

public class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; }
    public int HabitCount { get; set; }
}
