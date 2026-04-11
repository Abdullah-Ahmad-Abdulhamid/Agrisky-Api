using Agrisky.Models;
using AgriskyApi.Dtos;

namespace AgriskyApi.IRepo
{
    public interface IOwnerRepo:IGenricRepo<Owner>
    {
        Task<DashboardDto> GetDashboardData();
        Task<IEnumerable<OwnerOrderDto>> GetOrders();
        Task<bool> DeleteMessage(int messageId);
        Task<string> GetProofImage(int orderId);
    }
}
