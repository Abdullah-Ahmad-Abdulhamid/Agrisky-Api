using Agrisky.Models;
using AgriskyApi.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AgriskyApi.Services.Implementation
{
    public class JwtService : IJwtServices
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── Access Token ──────────────────────────────────────────────────────
        public string GenerateAccessToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"]
                ?? throw new InvalidOperationException("JWT key is not configured.");
            var issuer = jwtSection["Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var audience = jwtSection["Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");

            if (key.Length < 32)
                throw new InvalidOperationException("JWT key must be at least 256 bits (32 characters).");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Keep backward-compat alias (used in Google login path in AuthController)
        public string GenerateToken(User user) => GenerateAccessToken(user);

        // ── Refresh Token ─────────────────────────────────────────────────────
        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        // ── Extract principal from an expired token (for /refresh endpoint) ──
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"]
                ?? throw new InvalidOperationException("JWT key is not configured.");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,          // allow expired tokens here
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, validationParameters, out var securityToken);

                // Verify it is actually an HMAC-SHA256 token, not "alg: none"
                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("GetPrincipalFromExpiredToken: unexpected algorithm in token.");
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetPrincipalFromExpiredToken: token validation failed.");
                return null;
            }
        }
    }
}