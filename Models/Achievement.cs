namespace HabitTracker.Models;

public class Achievement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? HabitId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Habit? Habit { get; set; }
}
