namespace HabitTracker.Models;

public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? RepeatDays { get; set; }
    public string GoalType { get; set; } = "check";
    public double GoalValue { get; set; } = 1;
    public string? GoalUnit { get; set; }

    public Guid? CategoryId { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<HabitLog> HabitLogs { get; set; } = new List<HabitLog>();
}
