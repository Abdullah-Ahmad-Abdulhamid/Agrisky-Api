using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.EntityFrameworkCore;

namespace AgriskyApi.Repo
{
    public class UserRepo : GenricRepo<User>, IUserRepo
    {
        public UserRepo(AppDbcontext context) : base(context)
        {
        }

        public async Task<IEnumerable<ProductDto>> GetProducts(int? categoryId, string search)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search));

            return await query.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name,
                Images = p.ProductImages.Select(i => i.ImageURL).ToList()
            }).ToListAsync();
        }

        public async Task<bool> AddToCart(int userId, AddToCartDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null || dto.Quantity > product.StockQuantity)
                return false;

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == dto.ProductId);

            if (item != null)
                item.Quantity += dto.Quantity;
            else
            {
                item = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                await _context.CartItems.AddAsync(item);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CartItemDto>> GetCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return new List<CartItemDto>();

            return cart.CartItems.Select(ci => new CartItemDto
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                Quantity = ci.Quantity,
                Price = ci.Product.Price
            }).ToList();
        }

        public async Task<bool> SendMessage(ContactDto dto)
        {
            var msg = new ContactMessage
            {
                Name = dto.Name,
                Email = dto.Email,
                Message = dto.Message
            };

            await _context.ContactMessages.AddAsync(msg);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrders(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserID == userId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderDto
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount
                }).ToListAsync();
        }


        public async Task<bool> RegisterUserAsync(User user)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (exists) return false;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}