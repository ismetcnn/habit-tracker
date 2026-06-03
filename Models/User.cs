namespace HabitTracker.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; } = false;
    public bool IsBanned { get; set; } = false;

    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
