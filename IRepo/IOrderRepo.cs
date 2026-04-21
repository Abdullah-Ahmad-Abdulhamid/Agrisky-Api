using Agrisky.Models;
using AgriskyApi.Dtos;

namespace AgriskyApi.IRepo
{
    public interface IOrderRepo : IGenricRepo<Order>
    {
        /// <summary>
        /// Creates an order. UserId inside dto is always server-sourced (JWT).
        /// </summary>
        Task<Order> CreateOrder(CreateOrderDto dto, IFormFile? proof);

        /// <summary>
        /// Approves or rejects a payment proof submitted for an order.
        /// </summary>
        Task<bool> VerifyPayment(int orderId, bool approve, string? adminNote, string adminEmail);
    }
}