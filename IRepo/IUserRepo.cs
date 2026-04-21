using Agrisky.Models;
using AgriskyApi.Dtos;

namespace AgriskyApi.IRepo
{
    public interface IUserRepo : IGenricRepo<User>
    {
        Task<IEnumerable<ProductDto>> GetProducts(int? categoryId, string? search);
        Task<bool> AddToCart(int userId, AddToCartDto dto);
        Task<IEnumerable<CartItemDto>> GetCart(int userId);
        Task<IEnumerable<OrderDto>> GetUserOrders(int userId);
        Task<bool> SendMessage(ContactDto dto);
        Task<bool> RegisterUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task UpdateUserAsync(User user);
    }
}