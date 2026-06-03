using HabitTracker.Data;
using HabitTracker.DTOs;
using HabitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Services;

public class AchievementService
{
    private readonly AppDbContext _db;

    public AchievementService(AppDbContext db)
    {
        _db = db;
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

    private static int ToDayNumber(DayOfWeek dow) => dow == DayOfWeek.Sunday ? 7 : (int)dow;

    private static bool IsScheduled(DateOnly date, List<int> repeatDays) =>
        repeatDays.Count == 0 || repeatDays.Contains(ToDayNumber(date.DayOfWeek));

    public async Task<List<AchievementResponse>> CheckAndAwardAsync(Guid userId, Guid habitId, AppDbContext db)
    {
        var habit = await db.Habits.FirstOrDefaultAsync(h => h.Id == habitId);
        if (habit is null) return new List<AchievementResponse>();

        var logs = await db.HabitLogs
            .Where(l => l.HabitId == habitId)
            .Select(l => new { l.Date, l.Value })
            .ToListAsync();

        var completedDates = logs
            .GroupBy(l => l.Date)
            .Where(g => g.Sum(l => l.Value) >= habit.GoalValue)
            .Select(g => g.Key)
            .ToHashSet();

        int totalCompletions = completedDates.Count;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdDate = DateOnly.FromDateTime(habit.CreatedAt);
        var repeatDays = ParseRepeatDays(habit.RepeatDays);

        var todayScheduled = IsScheduled(today, repeatDays);
        var streakStart = (todayScheduled && !completedDates.Contains(today))
            ? today.AddDays(-1)
            : today;

        int currentStreak = 0;
        var check = streakStart;
        while (check >= createdDate)
        {
            if (IsScheduled(check, repeatDays))
            {
                if (completedDates.Contains(check))
                    currentStreak++;
                else
                    break;
            }
            check = check.AddDays(-1);
        }

        var earned = (await db.Achievements
            .Where(a => a.UserId == userId && a.HabitId == habitId)
            .Select(a => a.Type)
            .ToListAsync())
            .ToHashSet();

        var toAward = new List<Achievement>();

        void TryAward(string type, string title, string description)
        {
            if (earned.Contains(type)) return;
            toAward.Add(new Achievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HabitId = habitId,
                Type = type,
                Title = title,
                Description = description,
                EarnedAt = DateTime.UtcNow
            });
            earned.Add(type);
        }

        if (currentStreak >= 3)
            TryAward("streak_3", "3 Günlük Seri! 🔥", "Bir alışkanlığı 3 gün üst üste tamamladın");
        if (currentStreak >= 7)
            TryAward("streak_7", "7 Günlük Seri! 🔥", "Bir alışkanlığı 7 gün üst üste tamamladın");
        if (currentStreak >= 30)
            TryAward("streak_30", "30 Günlük Seri! 💪", "Bir alışkanlığı 30 gün üst üste tamamladın");
        if (currentStreak >= 100)
            TryAward("streak_100", "100 Günlük Seri! 🏆", "Bir alışkanlığı 100 gün üst üste tamamladın");

        if (totalCompletions >= 10)
            TryAward("total_10", "10 Tamamlama! ⭐", "Bir alışkanlığı 10 kez tamamladın");
        if (totalCompletions >= 50)
            TryAward("total_50", "50 Tamamlama! 🌟", "Bir alışkanlığı 50 kez tamamladın");
        if (totalCompletions >= 100)
            TryAward("total_100", "100 Tamamlama! 👑", "Bir alışkanlığı 100 kez tamamladın");

        // Perfect week: every scheduled day in the last 7 days was completed
        var last7Start = today.AddDays(-6);
        var scheduledLast7 = Enumerable.Range(0, 7)
            .Select(i => last7Start.AddDays(i))
            .Where(d => IsScheduled(d, repeatDays))
            .ToList();
        if (scheduledLast7.Count > 0 && scheduledLast7.All(d => completedDates.Contains(d)))
            TryAward("perfect_week", "Mükemmel Hafta! 🎯", "Son 7 günde planlanmış tüm alışkanlıkları tamamladın");

        if (toAward.Count > 0)
        {
            db.Achievements.AddRange(toAward);
            await db.SaveChangesAsync();
        }

        return toAward.Select(a => new AchievementResponse
        {
            Id = a.Id,
            Type = a.Type,
            Title = a.Title,
            Description = a.Description,
            EarnedAt = a.EarnedAt,
            HabitId = a.HabitId,
            HabitTitle = habit.Title
        }).ToList();
    }

    public async Task<(bool Success, string Message, List<AchievementResponse>? Data)> GetUserAchievementsAsync(Guid userId)
    {
        var achievements = await _db.Achievements
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.EarnedAt)
            .Select(a => new AchievementResponse
            {
                Id = a.Id,
                Type = a.Type,
                Title = a.Title,
                Description = a.Description,
                EarnedAt = a.EarnedAt,
                HabitId = a.HabitId,
                HabitTitle = a.Habit != null ? a.Habit.Title : null
            })
            .ToListAsync();

        return (true, string.Empty, achievements);
    }
}
