using Agrisky.Models;
using AgriskyApi.Data;
using Microsoft.EntityFrameworkCore;
using AgriskyApi.Dtos;
namespace AgriSky.API.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest req);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest req);
    Task<string?> UploadImageAsync(int userId, IFormFile file);
}

public class UserService(
    AppDbcontext db,
    ICacheService cache,
    IStorageService storage,
    ILogger<UserService> logger) : IUserService
{
    // ── GET (cache-first) ─────────────────────────────────────────────────
    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var cached = cache.Get<UserProfileDto>(CacheKeys.UserProfile(userId));
        if (cached is not null) return cached;

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user is null) return null;

        var dto = Map(user);
        cache.Set(CacheKeys.UserProfile(userId), dto, TimeSpan.FromMinutes(30));
        return dto;
    }

    // ── UPDATE ────────────────────────────────────────────────────────────
    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return null;

        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.PhoneNumber = req.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var dto = Map(user);
        cache.Set(CacheKeys.UserProfile(userId), dto, TimeSpan.FromMinutes(30));
        logger.LogInformation("Profile updated for user {UserId}", userId);
        return dto;
    }

    // ── CHANGE PASSWORD ───────────────────────────────────────────────────
    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Invalidate cache — user must re-authenticate
        cache.Remove(CacheKeys.UserProfile(userId));
        logger.LogInformation("Password changed for user {UserId}", userId);
        return true;
    }

    // ── UPLOAD IMAGE ──────────────────────────────────────────────────────
    public async Task<string?> UploadImageAsync(int userId, IFormFile file)
    {
        if (file.Length > 5 * 1024 * 1024)      // 5 MB limit
            throw new InvalidOperationException("File too large (max 5 MB).");

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException("Only JPEG, PNG, and WebP images are allowed.");

        var user = await db.Users.FindAsync(userId);
        if (user is null) return null;

        // Delete old image
        if (!string.IsNullOrEmpty(user.ProfilePicture))
            storage.Delete(user.ProfilePicture);

        var url = await storage.SaveAsync(file, $"profiles/{userId}");

        user.ProfilePicture = url;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Invalidate cache
        cache.Remove(CacheKeys.UserProfile(userId));
        logger.LogInformation("Profile picture updated for user {UserId}", userId);
        return url;
    }

    private static UserProfileDto Map(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber,
            u.Role, u.ProfilePicture, u.CreatedAt);
}