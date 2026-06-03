using System.ComponentModel.DataAnnotations;

namespace HabitTracker.DTOs;

public class CreateHabitRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<int>? RepeatDays { get; set; }

    public string GoalType { get; set; } = "check";
    public double GoalValue { get; set; } = 1;
    public string? GoalUnit { get; set; }

    public Guid? CategoryId { get; set; }
}

public class UpdateHabitRequest
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public List<int>? RepeatDays { get; set; }

    public string GoalType { get; set; } = "check";
    public double GoalValue { get; set; } = 1;
    public string? GoalUnit { get; set; }

    public Guid? CategoryId { get; set; }
}

public class CompleteHabitRequest
{
    public double Value { get; set; } = 1;
    public DateOnly? Date { get; set; }
}

public class CompleteHabitResponse
{
    public string Message { get; set; } = string.Empty;
    public List<AchievementResponse> NewAchievements { get; set; } = new();
}

public class HabitResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CompletedToday { get; set; }
    public List<int> RepeatDays { get; set; } = new();
    public bool IsScheduledToday { get; set; }
    public string GoalType { get; set; } = "check";
    public double GoalValue { get; set; } = 1;
    public string? GoalUnit { get; set; }
    public double TodayValue { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

public class HabitStatsResponse
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalCompletions { get; set; }
    public double SuccessRate { get; set; }
}

public class DailySummary
{
    public DateOnly Date { get; set; }
    public int CompletedCount { get; set; }
    public int TotalScheduled { get; set; }
    public double CompletionRate { get; set; }
}

public class HabitWeeklySummary
{
    public Guid HabitId { get; set; }
    public string HabitTitle { get; set; } = string.Empty;
    public List<DailySummary> Days { get; set; } = new();
}

public class OverallSummary
{
    public List<DailySummary> WeeklyData { get; set; } = new();
    public List<DailySummary> MonthlyData { get; set; } = new();
    public DailySummary? BestDay { get; set; }
    public int TotalActiveHabits { get; set; }
    public double AverageCompletionRate { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message, Data = default };
}
