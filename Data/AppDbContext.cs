using HabitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.HasIndex(u => u.Username)
                  .IsUnique();

            entity.HasMany(u => u.Habits)
                  .WithOne(h => h.User)
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Categories)
                  .WithOne(c => c.User)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Achievements)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(rt => rt.User)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.HasQueryFilter(h => !h.IsDeleted);

            entity.HasMany(h => h.HabitLogs)
                  .WithOne(hl => hl.Habit)
                  .HasForeignKey(hl => hl.HabitId)
                  .OnDelete(DeleteBehavior.Cascade);

            // No DB-level cascade; CategoryService explicitly nullifies CategoryId before delete
            entity.HasOne(h => h.Category)
                  .WithMany(c => c.Habits)
                  .HasForeignKey(h => h.CategoryId)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(a => a.Id);

            // Prevent duplicate achievements per habit per user
            entity.HasIndex(a => new { a.UserId, a.HabitId, a.Type })
                  .IsUnique();

            // No DB-level cascade; HabitService.DeleteAsync explicitly nullifies HabitId before delete
            entity.HasOne(a => a.Habit)
                  .WithMany()
                  .HasForeignKey(a => a.HabitId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HabitLog>(entity =>
        {
            entity.HasKey(hl => hl.Id);

            entity.HasIndex(hl => new { hl.HabitId, hl.Date })
                  .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.HasIndex(rt => rt.Token)
                  .IsUnique();
        });
    }
}
