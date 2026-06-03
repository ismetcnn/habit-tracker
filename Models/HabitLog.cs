namespace HabitTracker.Models;

public class HabitLog
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsCompleted { get; set; } = true;
    public double Value { get; set; } = 1;

    public Habit Habit { get; set; } = null!;
}
