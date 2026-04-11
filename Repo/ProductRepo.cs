using Agrisky.Models;
using AgriskyApi.IRepo;
using Microsoft.EntityFrameworkCore;

namespace AgriskyApi.Repo
{
    public class ProductRepo : GenricRepo<Product>, IProductRepo
    {
        public ProductRepo(AppDbcontext context) : base(context)
        {
        }
        public async Task<IEnumerable<Product>> GetAllWithFilter(string search, int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);

            return await query.ToListAsync();
        }

        public async Task<Product> GetWithImages(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductID == id);
        }
    }
}
