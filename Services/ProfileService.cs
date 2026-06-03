using HabitTracker.Data;
using HabitTracker.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Services;

public class ProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileResponse?> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new ProfileResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                TotalHabits = u.Habits.Count,
                TotalCompletions = u.Habits.SelectMany(h => h.HabitLogs).Count(),
                TotalAchievements = u.Achievements.Count
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<(bool Success, string Message, ProfileResponse? Data)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return (false, "User not found.", null);

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var taken = await _db.Users.AnyAsync(u => u.Username == request.Username && u.Id != userId);
            if (taken)
                return (false, "Username is already taken.", null);

            user.Username = request.Username;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var taken = await _db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId);
            if (taken)
                return (false, "Email is already in use.", null);

            user.Email = request.Email;
        }

        await _db.SaveChangesAsync();

        var profile = await GetProfileAsync(userId);
        return (true, "Profile updated.", profile);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return (false, "Şifreler eşleşmiyor.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return (false, "User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return (false, "Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));

        await _db.SaveChangesAsync();
        return (true, "Password changed successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAccountAsync(Guid userId, string password)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return (false, "User not found.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Invalid password.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return (true, "Account deleted.");
    }
}
