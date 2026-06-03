namespace HabitTracker.DTOs;

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalHabits { get; set; }
    public int TotalCompletionsToday { get; set; }
    public int ActiveStreaksCount { get; set; }
}

public class DailyCompletionStat
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class TopHabitStat
{
    public Guid HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CompletionCount { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
}

public class RecentAchievementStat
{
    public string AchievementTitle { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? HabitTitle { get; set; }
    public DateTime EarnedAt { get; set; }
}

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; }
    public int HabitCount { get; set; }
    public int TotalCompletions { get; set; }
    public DateOnly? LastActiveAt { get; set; }
}

public class AdminUserDetail : AdminUserDto
{
    public List<HabitResponse> Habits { get; set; } = new();
    public List<AchievementResponse> Achievements { get; set; } = new();
}

public class AdminHabitDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string GoalType { get; set; } = string.Empty;
    public int TotalCompletions { get; set; }
    public int CurrentStreak { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
