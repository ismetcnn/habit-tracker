using HabitTracker.Auth;
using HabitTracker.Data;
using HabitTracker.DTOs;
using HabitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthService(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = _jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return (false, "Email is already in use.", null);

        var usernameExists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameExists)
            return (false, "Username is already taken.", null);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return (true, "Registration successful.", new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            Username = user.Username,
            Email = user.Email
        });
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return (false, "Invalid email or password.", null);

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
            return (false, "Invalid email or password.", null);

        await _db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return (true, "Login successful.", new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            Username = user.Username,
            Email = user.Email
        });
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> RefreshAsync(RefreshTokenRequest request)
    {
        var stored = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken
                                    && !rt.IsRevoked
                                    && rt.ExpiresAt > DateTime.UtcNow);

        if (stored is null)
            return (false, "Geçersiz veya süresi dolmuş token.", null);

        stored.IsRevoked = true;
        await _db.SaveChangesAsync();

        var user = stored.User;
        var accessToken = _jwt.GenerateToken(user);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        return (true, "Token refreshed.", new AuthResponse
        {
            Token = accessToken,
            RefreshToken = newRefreshToken.Token,
            Username = user.Username,
            Email = user.Email
        });
    }
}
