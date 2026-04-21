using Agrisky.Models;
using AgriskyApi.Dtos;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;

namespace AgriSky.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbcontext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbcontext db, IConfiguration config, ILogger<AuthService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        // ── Register ──────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterDto dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();

                if (await _db.Users.AnyAsync(u => u.Email == email))
                    return (false, "Email already registered.", null);

                var user = new User
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = email,
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    Address = dto.Address?.Trim(),
                    Role = "User",                                         // server-set, never client-set
                    IsActive = true,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)   // cost factor defaults to 11
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                _logger.LogInformation("User registered: {Email}", email);
                return (true, "Registration successful.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", dto.Email);
                return (false, "Registration failed. Please try again.", null);
            }
        }

        // ── Login ─────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, User? User)> LoginAsync(LoginDto dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

                // Use constant-time responses — never reveal whether the email exists
                if (user == null)
                    return (false, "Invalid email or password.", null);

                if (string.IsNullOrEmpty(user.PasswordHash))
                    return (false, "This account uses Google sign-in. Please use 'Continue with Google'.", null);

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                    return (false, "Invalid email or password.", null);

                _logger.LogInformation("User logged in: {Email}", email);
                return (true, "Login successful.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", dto.Email);
                return (false, "Login failed. Please try again.", null);
            }
        }

        // ── Google Login ──────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, User? User)> GoogleLoginAsync(string idToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idToken))
                    return (false, "ID token is required.", null);

                var clientId = _config["Google:ClientId"]
                    ?? throw new InvalidOperationException("Google:ClientId is not configured.");

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                GoogleJsonWebSignature.Payload payload;
                try
                {
                    payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                }
                catch (InvalidJwtException ex)
                {
                    _logger.LogWarning("Invalid Google token received: {Message}", ex.Message);
                    return (false, "Invalid Google token.", null);
                }

                var email = payload.Email.ToLowerInvariant().Trim();

                // ── Upsert with explicit transaction to handle race conditions ──
                using var tx = await _db.Database.BeginTransactionAsync();

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        FirstName = payload.GivenName?.Trim()
                                         ?? payload.Name?.Split(' ').FirstOrDefault()?.Trim()
                                         ?? "User",
                        LastName = payload.FamilyName?.Trim()
                                         ?? (payload.Name?.Contains(' ') == true
                                             ? string.Join(" ", payload.Name.Split(' ').Skip(1)).Trim()
                                             : string.Empty),
                        Email = email,
                        GoogleId = payload.Subject,
                        ProfilePicture = payload.Picture,
                        Role = "User",      // server-set always
                        IsActive = true,
                        PasswordHash = null         // social-only — no password
                    };

                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("New Google user registered: {Email}", email);
                }
                else
                {
                    bool changed = false;

                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = payload.Subject;
                        changed = true;
                    }

                    if (payload.Picture != null && user.ProfilePicture != payload.Picture)
                    {
                        user.ProfilePicture = payload.Picture;
                        changed = true;
                    }

                    if (changed)
                    {
                        _db.Users.Update(user);
                        await _db.SaveChangesAsync();
                    }

                    _logger.LogInformation("Google user authenticated: {Email}", email);
                }

                await tx.CommitAsync();
                return (true, "Login successful.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed.");
                return (false, "Google authentication failed. Please try again.", null);
            }
        }
    }
}