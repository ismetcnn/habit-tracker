using HabitTracker.Data;
using HabitTracker.DTOs;
using HabitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Services;

public class AdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var totalUsers = await _db.Users.CountAsync();
        var totalHabits = await _db.Habits.CountAsync();
        var totalCompletionsToday = await _db.HabitLogs.CountAsync(l => l.Date == today);

        var habitIds = await _db.Habits
            .Select(h => new { h.Id, h.GoalValue, h.RepeatDays })
            .ToListAsync();

        var logsByHabit = await _db.HabitLogs
            .GroupBy(l => l.HabitId)
            .Select(g => new { HabitId = g.Key, Logs = g.Select(l => new { l.Date, l.Value }).ToList() })
            .ToListAsync();

        var logsLookup = logsByHabit.ToDictionary(x => x.HabitId, x => x.Logs);

        int activeStreaksCount = 0;
        foreach (var habit in habitIds)
        {
            if (!logsLookup.TryGetValue(habit.Id, out var logs) || logs.Count == 0)
                continue;

            var completedDates = logs
                .GroupBy(l => l.Date)
                .Where(g => g.Sum(l => l.Value) >= habit.GoalValue)
                .Select(g => g.Key)
                .ToHashSet();

            var repeatDays = ParseRepeatDays(habit.RepeatDays);
            var streak = ComputeCurrentStreak(completedDates, repeatDays, today);
            if (streak > 0)
                activeStreaksCount++;
        }

        return new AdminDashboardStats
        {
            TotalUsers = totalUsers,
            TotalHabits = totalHabits,
            TotalCompletionsToday = totalCompletionsToday,
            ActiveStreaksCount = activeStreaksCount
        };
    }

    public async Task<List<DailyCompletionStat>> GetLast7DaysCompletionsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-6);

        var counts = await _db.HabitLogs
            .Where(l => l.Date >= startDate && l.Date <= today)
            .GroupBy(l => l.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var lookup = counts.ToDictionary(x => x.Date, x => x.Count);

        var result = new List<DailyCompletionStat>();
        for (var d = startDate; d <= today; d = d.AddDays(1))
        {
            result.Add(new DailyCompletionStat
            {
                Date = d,
                Count = lookup.TryGetValue(d, out var c) ? c : 0
            });
        }

        return result;
    }

    public async Task<List<TopHabitStat>> GetTopHabitsAsync(int count = 5)
    {
        return await _db.HabitLogs
            .GroupBy(l => l.HabitId)
            .Select(g => new { HabitId = g.Key, CompletionCount = g.Count() })
            .OrderByDescending(x => x.CompletionCount)
            .Take(count)
            .Join(_db.Habits.Include(h => h.User),
                  stat => stat.HabitId,
                  habit => habit.Id,
                  (stat, habit) => new TopHabitStat
                  {
                      HabitId = habit.Id,
                      Title = habit.Title,
                      CompletionCount = stat.CompletionCount,
                      OwnerUsername = habit.User.Username
                  })
            .ToListAsync();
    }

    public async Task<List<RecentAchievementStat>> GetRecentAchievementsAsync(int count = 10)
    {
        return await _db.Achievements
            .OrderByDescending(a => a.EarnedAt)
            .Take(count)
            .Select(a => new RecentAchievementStat
            {
                AchievementTitle = a.Title,
                Username = a.User.Username,
                HabitTitle = a.Habit != null ? a.Habit.Title : null,
                EarnedAt = a.EarnedAt
            })
            .ToListAsync();
    }

    public async Task<PagedResult<AdminUserDto>> GetAllUsersAsync(string? search, int page, int pageSize = 10)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(s) || u.Email.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.CreatedAt,
                u.IsAdmin,
                u.IsBanned,
                HabitCount = u.Habits.Count,
                TotalCompletions = u.Habits.SelectMany(h => h.HabitLogs).Count(),
                LastActiveAt = u.Habits.SelectMany(h => h.HabitLogs).Max(l => (DateOnly?)l.Date)
            })
            .ToListAsync();

        return new PagedResult<AdminUserDto>
        {
            Items = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                IsAdmin = u.IsAdmin,
                IsBanned = u.IsBanned,
                HabitCount = u.HabitCount,
                TotalCompletions = u.TotalCompletions,
                LastActiveAt = u.LastActiveAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetail?> GetUserDetailAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Habits)
                .ThenInclude(h => h.Category)
            .Include(u => u.Habits)
                .ThenInclude(h => h.HabitLogs)
            .Include(u => u.Achievements)
                .ThenInclude(a => a.Habit)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habits = user.Habits.Select(h =>
        {
            var repeatDays = ParseRepeatDays(h.RepeatDays);
            var todayDayNumber = ToDayNumber(today.DayOfWeek);
            var todayValue = h.HabitLogs.Where(l => l.Date == today).Sum(l => l.Value);
            return new HabitResponse
            {
                Id = h.Id,
                Title = h.Title,
                Description = h.Description,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt,
                RepeatDays = repeatDays,
                IsScheduledToday = repeatDays.Count == 0 || repeatDays.Contains(todayDayNumber),
                GoalType = h.GoalType,
                GoalValue = h.GoalValue,
                GoalUnit = h.GoalUnit,
                TodayValue = todayValue,
                CompletedToday = todayValue >= h.GoalValue,
                CategoryId = h.CategoryId,
                CategoryName = h.Category?.Name
            };
        }).ToList();

        var achievements = user.Achievements.Select(a => new AchievementResponse
        {
            Id = a.Id,
            Type = a.Type,
            Title = a.Title,
            Description = a.Description,
            EarnedAt = a.EarnedAt,
            HabitId = a.HabitId,
            HabitTitle = a.Habit?.Title
        }).ToList();

        var totalCompletions = user.Habits.SelectMany(h => h.HabitLogs).Count();
        var lastActiveAt = user.Habits.SelectMany(h => h.HabitLogs)
            .Select(l => (DateOnly?)l.Date)
            .DefaultIfEmpty(null)
            .Max();

        return new AdminUserDetail
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            IsAdmin = user.IsAdmin,
            IsBanned = user.IsBanned,
            HabitCount = user.Habits.Count,
            TotalCompletions = totalCompletions,
            LastActiveAt = lastActiveAt,
            Habits = habits,
            Achievements = achievements
        };
    }

    public async Task<PagedResult<AdminHabitDto>> GetAllHabitsAsync(string? search, string? category, int page, int pageSize = 10)
    {
        var query = _db.Habits
            .Include(h => h.User)
            .Include(h => h.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(h => h.Title.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim().ToLower();
            query = query.Where(h => h.Category != null && h.Category.Name.ToLower().Contains(c));
        }

        var totalCount = await query.CountAsync();

        var habits = await query
            .OrderBy(h => h.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.Title,
                OwnerUsername = h.User.Username,
                CategoryName = h.Category != null ? h.Category.Name : null,
                h.GoalType,
                h.GoalValue,
                h.RepeatDays,
                h.CreatedAt,
                Logs = h.HabitLogs.Select(l => new { l.Date, l.Value }).ToList()
            })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = habits.Select(h =>
        {
            var completedDates = h.Logs
                .GroupBy(l => l.Date)
                .Where(g => g.Sum(l => l.Value) >= h.GoalValue)
                .Select(g => g.Key)
                .ToHashSet();

            var repeatDays = ParseRepeatDays(h.RepeatDays);
            var currentStreak = ComputeCurrentStreak(completedDates, repeatDays, today);

            return new AdminHabitDto
            {
                Id = h.Id,
                Title = h.Title,
                OwnerUsername = h.OwnerUsername,
                CategoryName = h.CategoryName,
                GoalType = h.GoalType,
                TotalCompletions = completedDates.Count,
                CurrentStreak = currentStreak
            };
        }).ToList();

        return new PagedResult<AdminHabitDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(bool Success, string Message)> BanUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return (false, "User not found.");

        user.IsBanned = true;
        await _db.SaveChangesAsync();
        return (true, "User banned.");
    }

    public async Task<(bool Success, string Message)> UnbanUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return (false, "User not found.");

        user.IsBanned = false;
        await _db.SaveChangesAsync();
        return (true, "User unbanned.");
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

    private static int ComputeCurrentStreak(HashSet<DateOnly> completedDates, List<int> repeatDays, DateOnly today)
    {
        var todayScheduled = repeatDays.Count == 0 || repeatDays.Contains(ToDayNumber(today.DayOfWeek));
        var streakStart = (todayScheduled && !completedDates.Contains(today))
            ? today.AddDays(-1)
            : today;

        int streak = 0;
        var check = streakStart;
        while (true)
        {
            if (repeatDays.Count == 0 || repeatDays.Contains(ToDayNumber(check.DayOfWeek)))
            {
                if (completedDates.Contains(check))
                    streak++;
                else
                    break;
            }
            check = check.AddDays(-1);
            if (check < today.AddDays(-3650))
                break;
        }

        return streak;
    }
}
