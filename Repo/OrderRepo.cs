using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using AgriskyApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AgriskyApi.Repo
{
    public class OrderRepo : GenricRepo<Order>, IOrderRepo
    {
        private readonly IFileValidationService _fileValidation;
        private readonly ILogger<OrderRepo> _logger;

        public OrderRepo(
            AppDbcontext context,
            IFileValidationService fileValidation,
            ILogger<OrderRepo> logger) : base(context)
        {
            _fileValidation = fileValidation;
            _logger = logger;
        }

        public async Task<Order> CreateOrder(CreateOrderDto dto, IFormFile? proof)
        {
            // ── dto.UserId is always JWT-sourced (set by the controller) ──────
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate payment method whitelist
                var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "VodafoneCash", "CashOnDelivery" };

                if (!allowedMethods.Contains(dto.PaymentMethod))
                    throw new InvalidOperationException("Invalid payment method.");

                // 2. Validate VodafoneCash transaction ID format
                if (dto.PaymentMethod.Equals("VodafoneCash", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(dto.VodafoneCashTransactionId) ||
                        !Regex.IsMatch(dto.VodafoneCashTransactionId, @"^\d{10,20}$"))
                    {
                        throw new InvalidOperationException(
                            "A valid VodafoneCash transaction ID (10–20 digits) is required.");
                    }
                }

                // 3. Shipping upsert
                var shipping = await _context.Shippings
                    .FirstOrDefaultAsync(s => s.UserID == dto.UserId);

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

                // 4. Load products server-side — prices are NEVER taken from the client
                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                decimal subtotal = 0m;
                foreach (var item in dto.Items)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == item.ProductId)
                        ?? throw new InvalidOperationException(
                               $"Product {item.ProductId} not found.");

                    if (product.StockQuantity < item.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for: {product.Name}");

                    subtotal += product.Price * item.Quantity;
                }

                // 5. Create the order — Status and TotalAmount are server-set
                const decimal shippingFee = 7.70m;
                var order = new Order
                {
                    UserID = dto.UserId,
                    ShippingID = shipping.ShippingID,
                    TotalAmount = subtotal + shippingFee,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    PaymentMethod = dto.PaymentMethod,
                    IsSeenByAdmin = false
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 6. Create order items and deduct stock
                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.ProductID == item.ProductId);
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderID = order.OrderID,
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        Price = product.Price   // server price, never client price
                    });
                    product.StockQuantity -= item.Quantity;
                }

                // 7. Save proof image (validated via IFileValidationService)
                string? proofPath = null;
                if (dto.PaymentMethod.Equals("VodafoneCash", StringComparison.OrdinalIgnoreCase)
                    && proof != null)
                {
                    // SaveProofAsync validates file AND saves outside wwwroot
                    proofPath = await _fileValidation.SaveProofAsync(proof, order.OrderID);
                }

                // 8. Create Payment record — Status always server-determined
                var paymentStatus = dto.PaymentMethod.Equals("VodafoneCash", StringComparison.OrdinalIgnoreCase)
                    ? PaymentStatus.PendingVerification
                    : PaymentStatus.AwaitingPayment;

                var payment = new Payment
                {
                    OrderID = order.OrderID,
                    AmountPaid = order.TotalAmount,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Status = paymentStatus.ToString(),
                    TransactionReference = dto.VodafoneCashTransactionId,
                    ProofImagePath = proofPath
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} created for user {UserId} — method: {Method} — total: {Total}",
                    order.OrderID, dto.UserId, dto.PaymentMethod, order.TotalAmount);

                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Order creation failed for user {UserId}", dto.UserId);
                throw;
            }
        }

        // ── Admin: approve or reject a payment proof ──────────────────────────
        public async Task<bool> VerifyPayment(
            int orderId, bool approve, string? adminNote, string adminEmail)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderID == orderId);

            if (payment == null) return false;

            // Only allow verification on PendingVerification payments
            if (payment.Status != PaymentStatus.PendingVerification.ToString())
                return false;

            payment.Status = approve
                ? PaymentStatus.Paid.ToString()
                : PaymentStatus.Rejected.ToString();
            payment.AdminNote = adminNote;

            if (approve)
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = "Processing";
                    order.UpdatedAt = DateTime.UtcNow;
                    order.UpdatedBy = adminEmail;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Payment for order {OrderId} {Action} by {Admin}",
                orderId, approve ? "approved" : "rejected", adminEmail);

            return true;
        }
    }
}