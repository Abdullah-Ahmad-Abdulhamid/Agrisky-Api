using Agrisky.Models;
using AgriskyApi.Dtos;

namespace AgriSky.API.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string Message, User? User)> LoginAsync(LoginDto dto);
        Task<(bool Success, string Message, User? User)> GoogleLoginAsync(string idToken);
    }

}