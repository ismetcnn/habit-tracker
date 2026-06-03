namespace HabitTracker.DTOs;

public class AchievementResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; }
    public Guid? HabitId { get; set; }
    public string? HabitTitle { get; set; }
}
