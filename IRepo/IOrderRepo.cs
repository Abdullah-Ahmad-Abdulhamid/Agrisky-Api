using Agrisky.Models;
using AgriskyApi.Dtos;

namespace AgriskyApi.IRepo
{
    public interface IOrderRepo : IGenricRepo<Order>
    {
        Task<Order> CreateOrder(CreateOrderDto dto, IFormFile? proof);
    }
}