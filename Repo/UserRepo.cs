using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace AgriskyApi.Repo
{
    public class UserRepo : GenricRepo<User>, IUserRepo
    {
        private readonly IDistributedCache _cache;

        public UserRepo(AppDbcontext context, IDistributedCache cache) : base(context)
        {
            _cache = cache;
        }

        // ── Cart Operations (cache-optimised) ─────────────────────────────────

        public async Task<IEnumerable<CartItemDto>> GetCart(int userId)
        {
            string cacheKey = $"cart_{userId}";

            var cachedCart = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedCart))
                return JsonConvert.DeserializeObject<IEnumerable<CartItemDto>>(cachedCart)
                       ?? Enumerable.Empty<CartItemDto>();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return Enumerable.Empty<CartItemDto>();

            var cartData = cart.CartItems.Select(ci => new CartItemDto
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                Quantity = ci.Quantity,
                Price = ci.Product.Price   // always from DB
            }).ToList();

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));

            await _cache.SetStringAsync(cacheKey,
                JsonConvert.SerializeObject(cartData), cacheOptions);

            return cartData;
        }

        public async Task<bool> AddToCart(int userId, AddToCartDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null || dto.Quantity > product.StockQuantity)
                return false;

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId
                                        && ci.ProductId == dto.ProductId);
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

            var success = await _context.SaveChangesAsync() > 0;
            if (success)
                await _cache.RemoveAsync($"cart_{userId}");

            return success;
        }

        // ── Product & Order Methods ───────────────────────────────────────────

        public async Task<IEnumerable<ProductDto>> GetProducts(int? categoryId, string? search)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsNoTracking()
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(lower) ||
                    (p.Description != null && p.Description.ToLower().Contains(lower)));
            }

            return await query.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryName = p.Category.Name,
                Images = p.ProductImages.Select(i => i.ImageURL).ToList()
            }).ToListAsync();
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrders(int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .Where(o => o.UserID == userId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderDto
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.Payment != null ? o.Payment.Status : string.Empty,
                    Items = o.OrderItems.Select(oi => new OrderItemSummaryDto
                    {
                        ProductName = oi.Product.Name,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.Price
                    }).ToList()
                }).ToListAsync();
        }

        // ── User & Support Methods ────────────────────────────────────────────

        public async Task<bool> SendMessage(ContactDto dto)
        {
            var message = new ContactMessage
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Message = dto.Message.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            await _context.ContactMessages.AddAsync(message);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RegisterUserAsync(User user)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Email == user.Email);
            if (exists) return false;

            await _context.Users.AddAsync(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
            => await _context.Users
                   .FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetUserByIdAsync(int id)
            => await _context.Users.FindAsync(id);

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}