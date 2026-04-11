using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.EntityFrameworkCore;

namespace AgriskyApi.Repo
{
    public class OrderRepo : GenricRepo<Order>, IOrderRepo
    {
        private readonly IWebHostEnvironment _env;

        public OrderRepo(AppDbcontext context, IWebHostEnvironment env) : base(context)
        {
            _env = env;
        }

        public async Task<Order> CreateOrder(CreateOrderDto dto, IFormFile? proof)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Update or create shipping info
                var shipping = await _context.Shippings.FirstOrDefaultAsync(s => s.UserID == dto.UserId);
                if (shipping != null)
                {
                    shipping.Address = dto.Shipping.Address;
                    shipping.City = dto.Shipping.City;
                    shipping.Country = dto.Shipping.Country;
                    shipping.ZipCode = dto.Shipping.ZipCode;
                    shipping.PhoneNumber = dto.Shipping.PhoneNumber;
                }
                else
                {
                    shipping = new Shipping
                    {
                        UserID = dto.UserId,
                        Address = dto.Shipping.Address,
                        City = dto.Shipping.City,
                        Country = dto.Shipping.Country,
                        ZipCode = dto.Shipping.ZipCode,
                        PhoneNumber = dto.Shipping.PhoneNumber
                    };
                    _context.Shippings.Add(shipping);
                }
                await _context.SaveChangesAsync();

                // 2. Validate products and stock
                var productIds = dto.Items.Select(i => i.ProductId).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                decimal subtotal = 0;
                foreach (var item in dto.Items)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == item.ProductId);
                    if (product == null)
                        throw new Exception($"المنتج رقم {item.ProductId} غير موجود.");
                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"المخزون غير كافٍ للمنتج: {product.Name}");

                    subtotal += product.Price * item.Quantity;
                }

                // 3. Create the order (subtotal + fixed shipping fee)
                decimal total = subtotal + 7.7m;
                var order = new Order
                {
                    UserID = dto.UserId,
                    ShippingID = shipping.ShippingID,
                    TotalAmount = total,
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    PaymentMethod = dto.PaymentMethod,
                    IsDeleted = false,
                    IsSeenByAdmin = false
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 4. Add order items and reduce stock
                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.ProductID == item.ProductId);
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderID = order.OrderID,
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        Price = product.Price
                    });
                    product.StockQuantity -= item.Quantity;
                }

                // 5. Handle payment proof (VodafoneCash only)
                string proofPath = string.Empty;
                if (dto.PaymentMethod == "VodafoneCash" && proof != null)
                {
                    // Fallback if WebRootPath is null (no wwwroot folder)
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsDir = Path.Combine(webRoot, "proofs");

                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(proof.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await proof.CopyToAsync(stream);
                    }

                    proofPath = "/proofs/" + fileName;
                }

                // 6. Record the payment
                var payment = new Payment
                {
                    OrderID = order.OrderID,
                    AmountPaid = total,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentDate = DateTime.Now,
                    Status = dto.PaymentMethod == "VodafoneCash"
                        ? "Pending Verification"
                        : "Awaiting Payment",
                    ProofImagePath = proofPath
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception(ex.Message);
            }
        }
    }
}