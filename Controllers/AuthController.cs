using Agrisky.Models;
using AgriSky.API.Services;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using AgriskyApi.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepo _repo;
        private readonly IJwtServices _jwtService;
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;   // ← properly injected

        public AuthController(
            IUserRepo repo,
            IJwtServices jwtService,
            IAuthService authService,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _repo = repo;
            _jwtService = jwtService;
            _authService = authService;
            _config = config;
            _logger = logger;
        }

        // ── POST /api/auth/register ───────────────────────────────────────────
        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.RegisterAsync(model);

            if (!success)
                return Conflict(new { message });

            _logger.LogInformation("New user registered: {Email} from IP {IP}",
                model.Email, HttpContext.Connection.RemoteIpAddress);

            // Do NOT auto-login on register — require an explicit login step.
            return Ok(new { message = "Registration successful. Please log in." });
        }

        // ── POST /api/auth/login ──────────────────────────────────────────────
        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, user) = await _authService.LoginAsync(model);

            if (!success || user == null)
            {
                // Structured failure log for brute-force monitoring
                _logger.LogWarning(
                    "Failed login for {Email} from IP {IP} at {Time}",
                    model.Email,
                    HttpContext.Connection.RemoteIpAddress,
                    DateTime.UtcNow);

                // Constant-time response — do not reveal whether email exists
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is suspended. Please contact support." });

            // 1. Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // 2. Persist hashed refresh token
            user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _repo.UpdateUserAsync(user);

            // 3. Set HttpOnly, Secure, SameSite=Strict cookies — tokens never in body
            SetAccessTokenCookie(accessToken);
            SetRefreshTokenCookie(refreshToken);

            _logger.LogInformation(
                "User {UserId} ({Email}) logged in from IP {IP}",
                user.Id, user.Email, HttpContext.Connection.RemoteIpAddress);

            return Ok(new
            {
                message = "Login successful.",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.ProfilePicture
                }
            });
        }

        // ── POST /api/auth/refresh ────────────────────────────────────────────
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            // 1. Get both tokens from HttpOnly cookies only
            if (!Request.Cookies.TryGetValue("accessToken", out var expiredAccessToken)
                || string.IsNullOrEmpty(expiredAccessToken))
                return Unauthorized(new { message = "Access token cookie missing." });

            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken)
                || string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "Refresh token cookie missing." });

            // 2. Validate the expired access token structure (signature still checked)
            var principal = _jwtService.GetPrincipalFromExpiredToken(expiredAccessToken);
            if (principal == null)
                return Unauthorized(new { message = "Invalid access token." });

            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Invalid token claims." });

            // 3. Load user and verify refresh token expiry
            var user = await _repo.GetUserByIdAsync(userId);
            if (user == null
                || user.RefreshTokenExpiryDate == null
                || user.RefreshTokenExpiryDate < DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token has expired. Please log in again." });

            // 4. Verify the refresh token matches the stored hash
            if (string.IsNullOrEmpty(user.RefreshToken)
                || !BCrypt.Net.BCrypt.Verify(refreshToken, user.RefreshToken))
                return Unauthorized(new { message = "Invalid refresh token." });

            // 5. Rotate both tokens (refresh token rotation)
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _repo.UpdateUserAsync(user);

            SetAccessTokenCookie(newAccessToken);
            SetRefreshTokenCookie(newRefreshToken);

            return Ok(new { message = "Token refreshed." });
        }

        // ── POST /api/auth/logout ─────────────────────────────────────────────
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
                return BadRequest(new { message = "Invalid token." });

            // Invalidate the refresh token in the database
            var user = await _repo.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryDate = null;
                await _repo.UpdateUserAsync(user);
            }

            // Clear both cookies
            DeleteAuthCookies();

            _logger.LogInformation(
                "User {UserId} logged out from IP {IP}",
                userId, HttpContext.Connection.RemoteIpAddress);

            return Ok(new { message = "Logged out successfully." });
        }

        // ── POST /api/auth/google ─────────────────────────────────────────────
        /// <summary>
        /// Accepts a Google ID token from the React client (@react-oauth/google).
        /// Delegates all verification and user provisioning to IAuthService.
        /// The old controller had a duplicate inline implementation — removed.
        /// </summary>
        [HttpPost("google")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.IdToken))
                return BadRequest(new { message = "ID token is required." });

            // ── Delegate entirely to IAuthService — no duplicate logic here ───
            var (success, message, user) = await _authService.GoogleLoginAsync(dto.IdToken);

            if (!success || user == null)
                return Unauthorized(new { message });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is suspended. Please contact support." });

            // Issue tokens via cookies (same flow as standard login)
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await _repo.UpdateUserAsync(user);

            SetAccessTokenCookie(accessToken);
            SetRefreshTokenCookie(refreshToken);

            _logger.LogInformation(
                "Google user {Email} authenticated from IP {IP}",
                user.Email, HttpContext.Connection.RemoteIpAddress);

            return Ok(new
            {
                message = "Login successful.",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.ProfilePicture
                }
            });
        }

        // ── Cookie helpers ────────────────────────────────────────────────────
        private void SetAccessTokenCookie(string token)
        {
            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });
        }

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/api/auth/refresh"  // Scope refresh token to refresh endpoint only
            });
        }

        private void DeleteAuthCookies()
        {
            Response.Cookies.Delete("accessToken",
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            Response.Cookies.Delete("refreshToken",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api/auth/refresh"
                });
        }
    }

    // ── Google login DTO ──────────────────────────────────────────────────────
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}