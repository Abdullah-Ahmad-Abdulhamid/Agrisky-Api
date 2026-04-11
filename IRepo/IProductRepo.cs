using Agrisky.Models;

namespace AgriskyApi.IRepo
{
    public interface IProductRepo:IGenricRepo<Product>
    {
        Task<IEnumerable<Product>> GetAllWithFilter(string search, int? categoryId);
        Task<Product> GetWithImages(int id);
    }
}
