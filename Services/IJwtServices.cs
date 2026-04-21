using Agrisky.Models;
using System.Security.Claims;

namespace AgriskyApi.Services
{
    public interface IJwtServices
    {
        /// <summary>Generates a short-lived access token (15 min).</summary>
        string GenerateAccessToken(User user);

        /// <summary>Generates a cryptographically random opaque refresh token.</summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Validates an expired access token and returns its ClaimsPrincipal.
        /// Used exclusively by the /auth/refresh endpoint.
        /// Returns null if the token signature is invalid.
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        // Keep backward-compat alias used in Google login path
        string GenerateToken(User user);
    }
}