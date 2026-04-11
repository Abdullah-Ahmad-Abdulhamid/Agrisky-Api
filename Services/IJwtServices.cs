using Agrisky.Models;

namespace AgriskyApi.Services
{
    public interface IJwtServices
    {
        string GenerateToken(User user);

    }
}
