using HabitTracker.Data;
using HabitTracker.DTOs;
using HabitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Services;

public class HabitService
{
    private readonly AppDbContext _db;
    private readonly AchievementService _achievementService;

    public HabitService(AppDbContext db, AchievementService achievementService)
    {
        _db = db;
        _achievementService = achievementService;
    }

    private static List<int> ParseRepeatDays(string? repeatDays)
    {
        if (string.IsNullOrWhiteSpace(repeatDays))
            return new List<int>();
        return repeatDays.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n >= 1 && n <= 7)
            .ToList();
    }

    private static string? FormatRepeatDays(List<int>? repeatDays)
    {
        if (repeatDays is null || repeatDays.Count == 0)
            return null;
        var valid = repeatDays.Where(d => d >= 1 && d <= 7).Distinct().OrderBy(d => d).ToList();
        return valid.Count == 0 ? null : string.Join(",", valid);
    }

    private static int ToDayNumber(DayOfWeek dow) => dow == DayOfWeek.Sunday ? 7 : (int)dow;

    private static bool IsScheduled(DateOnly date, List<int> repeatDays) =>
        repeatDays.Count == 0 || repeatDays.Contains(ToDayNumber(date.DayOfWeek));

    private static HabitResponse MapToResponse(Habit habit, double todayValue, DateOnly today)
    {
        var repeatDays = ParseRepeatDays(habit.RepeatDays);
        var todayDayNumber = ToDayNumber(today.DayOfWeek);
        return new HabitResponse
        {
            Id = habit.Id,
            Title = habit.Title,
            Description = habit.Description,
            IsActive = habit.IsActive,
            CreatedAt = habit.CreatedAt,
            RepeatDays = repeatDays,
            IsScheduledToday = repeatDays.Count == 0 || repeatDays.Contains(todayDayNumber),
            GoalType = habit.GoalType,
            GoalValue = habit.GoalValue,
            GoalUnit = habit.GoalUnit,
            TodayValue = todayValue,
            CompletedToday = todayValue >= habit.GoalValue,
            CategoryId = habit.CategoryId,
            CategoryName = habit.Category?.Name
        };
    }

    public async Task<(bool Success, string Message, List<HabitResponse>? Data)> GetUserHabitsAsync(Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habits = await _db.Habits
            .Where(h => h.UserId == userId && h.IsActive)
            .Include(h => h.HabitLogs)
            .Include(h => h.Category)
            .ToListAsync();

        var result = habits.Select(h =>
        {
            var todayValue = h.HabitLogs
                .Where(l => l.Date == today)
                .Sum(l => l.Value);
            return MapToResponse(h, todayValue, today);
        }).ToList();

        return (true, string.Empty, result);
    }

    public async Task<(bool Success, string Message, HabitResponse? Data)> CreateAsync(Guid userId, CreateHabitRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == userId);
            if (!categoryExists)
                return (false, "Category not found.", null);
        }

        var habit = new Habit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RepeatDays = FormatRepeatDays(request.RepeatDays),
            GoalType = request.GoalType,
            GoalValue = request.GoalValue,
            GoalUnit = request.GoalUnit,
            CategoryId = request.CategoryId
        };

        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();

        if (habit.CategoryId.HasValue)
            await _db.Entry(habit).Reference(h => h.Category).LoadAsync();

        return (true, "Habit created.", MapToResponse(habit, 0, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    public async Task<(bool Success, string Message, HabitResponse? Data)> UpdateAsync(Guid userId, Guid habitId, UpdateHabitRequest request)
    {
        var habit = await _db.Habits
            .Include(h => h.Category)
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null)
            return (false, "Habit not found.", null);

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == userId);
            if (!categoryExists)
                return (false, "Category not found.", null);
        }

        habit.Title = request.Title;
        habit.Description = request.Description;
        habit.IsActive = request.IsActive;
        habit.RepeatDays = FormatRepeatDays(request.RepeatDays);
        habit.GoalType = request.GoalType;
        habit.GoalValue = request.GoalValue;
        habit.GoalUnit = request.GoalUnit;
        habit.CategoryId = request.CategoryId;

        await _db.SaveChangesAsync();

        if (habit.CategoryId.HasValue && habit.Category?.Id != habit.CategoryId)
            await _db.Entry(habit).Reference(h => h.Category).LoadAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayValue = await _db.HabitLogs
            .Where(l => l.HabitId == habitId && l.Date == today)
            .SumAsync(l => (double?)l.Value) ?? 0;

        return (true, "Habit updated.", MapToResponse(habit, todayValue, today));
    }

    public async Task<(bool Success, string Message, object? Data)> DeleteAsync(Guid userId, Guid habitId)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null)
            return (false, "Habit not found.", null);

        habit.IsDeleted = true;
        habit.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, "Habit deleted.", null);
    }

    public async Task<(bool Success, string Message, List<HabitResponse>? Data)> GetDeletedHabitsAsync(Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habits = await _db.Habits
            .IgnoreQueryFilters()
            .Where(h => h.UserId == userId && h.IsDeleted)
            .Include(h => h.HabitLogs)
            .Include(h => h.Category)
            .ToListAsync();

        var result = habits.Select(h =>
        {
            var todayValue = h.HabitLogs
                .Where(l => l.Date == today)
                .Sum(l => l.Value);
            return MapToResponse(h, todayValue, today);
        }).ToList();

        return (true, string.Empty, result);
    }

    public async Task<(bool Success, string Message)> RestoreHabitAsync(Guid userId, Guid habitId)
    {
        var habit = await _db.Habits
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId && h.IsDeleted);

        if (habit is null)
            return (false, "Deleted habit not found.");

        habit.IsDeleted = false;
        habit.DeletedAt = null;
        await _db.SaveChangesAsync();

        return (true, "Habit restored.");
    }

    public async Task<(bool Success, string Message, HabitStatsResponse? Data)> GetStatsAsync(Guid userId, Guid habitId)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null)
            return (false, "Habit not found.", null);

        var logs = await _db.HabitLogs
            .Where(l => l.HabitId == habitId)
            .Select(l => new { l.Date, l.Value })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Days where the daily value sum met the goal
        var completedDates = logs
            .GroupBy(l => l.Date)
            .Where(g => g.Sum(l => l.Value) >= habit.GoalValue)
            .Select(g => g.Key)
            .ToHashSet();

        int totalCompletions = completedDates.Count;

        var scheduledDays = ParseRepeatDays(habit.RepeatDays);
        var createdDate = DateOnly.FromDateTime(habit.CreatedAt);

        int totalScheduledDays = 0;
        for (var d = createdDate; d <= today; d = d.AddDays(1))
        {
            if (IsScheduled(d, scheduledDays))
                totalScheduledDays++;
        }

        var successRate = totalScheduledDays > 0
            ? Math.Round((double)totalCompletions / totalScheduledDays * 100, 1)
            : 0;

        // Current streak: grace period if today is scheduled but not completed yet
        var todayScheduled = IsScheduled(today, scheduledDays);
        var streakStart = (todayScheduled && !completedDates.Contains(today))
            ? today.AddDays(-1)
            : today;

        int currentStreak = 0;
        var check = streakStart;
        while (check >= createdDate)
        {
            if (IsScheduled(check, scheduledDays))
            {
                if (completedDates.Contains(check))
                    currentStreak++;
                else
                    break;
            }
            check = check.AddDays(-1);
        }

        // Longest streak: non-scheduled days are transparent
        int longestStreak = 0;
        int run = 0;
        for (var d = createdDate; d <= today; d = d.AddDays(1))
        {
            if (IsScheduled(d, scheduledDays))
            {
                if (completedDates.Contains(d))
                {
                    run++;
                    if (run > longestStreak) longestStreak = run;
                }
                else
                {
                    run = 0;
                }
            }
        }

        return (true, string.Empty, new HabitStatsResponse
        {
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            TotalCompletions = totalCompletions,
            SuccessRate = successRate
        });
    }

    public async Task<(bool Success, string Message, CompleteHabitResponse? Data)> CompleteAsync(Guid userId, Guid habitId, CompleteHabitRequest request)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null)
            return (false, "Habit not found.", null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDate = request.Date ?? today;

        if (targetDate > today)
            return (false, "Gelecek bir tarih için tamamlama yapılamaz.", null);

        if (targetDate < today.AddDays(-7))
            return (false, "7 günden eski tarihler için tamamlama yapılamaz.", null);

        var repeatDays = ParseRepeatDays(habit.RepeatDays);
        var targetDayNumber = ToDayNumber(targetDate.DayOfWeek);

        if (repeatDays.Count > 0 && !repeatDays.Contains(targetDayNumber))
            return (false, "Bu alışkanlık bugün planlanmamış.", null);

        var existingLog = await _db.HabitLogs
            .FirstOrDefaultAsync(l => l.HabitId == habitId && l.Date == targetDate);

        if (habit.GoalType == "check")
        {
            if (existingLog is not null)
                return (false, "Habit already completed today.", null);

            _db.HabitLogs.Add(new HabitLog
            {
                Id = Guid.NewGuid(),
                HabitId = habitId,
                Date = targetDate,
                IsCompleted = true,
                Value = 1
            });
        }
        else
        {
            if (existingLog is not null)
                existingLog.Value += request.Value;
            else
                _db.HabitLogs.Add(new HabitLog
                {
                    Id = Guid.NewGuid(),
                    HabitId = habitId,
                    Date = targetDate,
                    IsCompleted = true,
                    Value = request.Value
                });
        }

        await _db.SaveChangesAsync();

        var newAchievements = await _achievementService.CheckAndAwardAsync(userId, habitId, _db);
        const string completionMessage = "Habit marked as complete.";

        return (true, completionMessage, new CompleteHabitResponse
        {
            Message = completionMessage,
            NewAchievements = newAchievements
        });
    }

    public async Task<(bool Success, string Message, OverallSummary? Data)> GetOverallSummaryAsync(Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-29);

        var habits = await _db.Habits
            .Where(h => h.UserId == userId && h.IsActive)
            .Select(h => new
            {
                h.RepeatDays,
                h.GoalValue,
                Logs = h.HabitLogs
                    .Where(l => l.Date >= startDate && l.Date <= today)
                    .Select(l => new { l.Date, l.Value })
                    .ToList()
            })
            .ToListAsync();

        var habitData = habits.Select(h =>
        {
            var repeatDays = ParseRepeatDays(h.RepeatDays);
            var dailyValues = h.Logs
                .GroupBy(l => l.Date)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Value));
            return (repeatDays, h.GoalValue, dailyValues);
        }).ToList();

        var monthlyData = new List<DailySummary>();
        for (var d = startDate; d <= today; d = d.AddDays(1))
        {
            int totalScheduled = 0;
            int completedCount = 0;

            foreach (var (repeatDays, goalValue, dailyValues) in habitData)
            {
                if (IsScheduled(d, repeatDays))
                {
                    totalScheduled++;
                    var dayValue = dailyValues.TryGetValue(d, out var v) ? v : 0;
                    if (dayValue >= goalValue)
                        completedCount++;
                }
            }

            monthlyData.Add(new DailySummary
            {
                Date = d,
                CompletedCount = completedCount,
                TotalScheduled = totalScheduled,
                CompletionRate = totalScheduled > 0
                    ? Math.Round((double)completedCount / totalScheduled * 100, 1)
                    : 0
            });
        }

        var scheduledDays = monthlyData.Where(d => d.TotalScheduled > 0).ToList();
        var bestDay = scheduledDays.Count > 0
            ? scheduledDays.MaxBy(d => d.CompletionRate)
            : null;
        var averageCompletionRate = scheduledDays.Count > 0
            ? Math.Round(scheduledDays.Average(d => d.CompletionRate), 1)
            : 0;

        return (true, string.Empty, new OverallSummary
        {
            MonthlyData = monthlyData,
            WeeklyData = monthlyData.Skip(23).ToList(),
            BestDay = bestDay,
            TotalActiveHabits = habits.Count,
            AverageCompletionRate = averageCompletionRate
        });
    }

    public async Task<(bool Success, string Message, HabitWeeklySummary? Data)> GetHabitWeeklySummaryAsync(Guid userId, Guid habitId)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null)
            return (false, "Habit not found.", null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-6);

        var logs = await _db.HabitLogs
            .Where(l => l.HabitId == habitId && l.Date >= startDate && l.Date <= today)
            .Select(l => new { l.Date, l.Value })
            .ToListAsync();

        var dailyValues = logs
            .GroupBy(l => l.Date)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Value));

        var repeatDays = ParseRepeatDays(habit.RepeatDays);
        var days = new List<DailySummary>();

        for (var d = startDate; d <= today; d = d.AddDays(1))
        {
            var scheduled = IsScheduled(d, repeatDays);
            var dayValue = dailyValues.TryGetValue(d, out var v) ? v : 0;
            var completed = scheduled && dayValue >= habit.GoalValue;

            days.Add(new DailySummary
            {
                Date = d,
                TotalScheduled = scheduled ? 1 : 0,
                CompletedCount = completed ? 1 : 0,
                CompletionRate = completed ? 100 : 0
            });
        }

        return (true, string.Empty, new HabitWeeklySummary
        {
            HabitId = habit.Id,
            HabitTitle = habit.Title,
            Days = days
        });
    }
}
