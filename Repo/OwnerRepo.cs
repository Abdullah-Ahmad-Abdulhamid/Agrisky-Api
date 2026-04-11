using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.EntityFrameworkCore;

namespace AgriskyApi.Repo
{
    public class OwnerRepo : GenricRepo<Owner>, IOwnerRepo
    {
        private readonly AppDbcontext _context;

        public OwnerRepo(AppDbcontext context):base(context) 
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboardData()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var totalSalesToday = await _context.Orders
                .Where(o => o.OrderDate.Date == today && o.Status == "Delivered")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalSalesMonth = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.Status == "Delivered")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalSoldProducts = await _context.OrderItems
                .Where(oi => oi.Order.Status == "Delivered")
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            var totalCancelledOrders = await _context.Orders
                .CountAsync(o => o.Status == "Cancelled");

            var bestSelling = await _context.OrderItems
                .GroupBy(oi => oi.ProductID)
                .Select(g => new
                {
                    Name = g.First().Product.Name,
                    Qty = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Qty)
                .FirstOrDefaultAsync();

            var worstSelling = await _context.OrderItems
                .GroupBy(oi => oi.ProductID)
                .Select(g => new
                {
                    Name = g.First().Product.Name,
                    Qty = g.Sum(x => x.Quantity)
                })
                .OrderBy(x => x.Qty)
                .FirstOrDefaultAsync();

            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.SubmittedAt)
                .Take(5)
                .Select(m => new ContactMessageDto
                {
                    Id = m.Id,
                    Email = m.Email,
                    Message = m.Message,
                    SubmittedAt = m.SubmittedAt
                }).ToListAsync();

            var feedbacks = await _context.Feedbacks
                .Include(f => f.Product)
                .Include(f => f.User)
                .OrderByDescending(f => f.DateSubmitted)
                .Take(5)
                .Select(f => new FeedbackDto
                {
                    UserName = f.User.FirstName,
                    ProductName = f.Product.Name,
                    Comment = f.Comment,
                    DateSubmitted = f.DateSubmitted
                }).ToListAsync();

            return new DashboardDto
            {
                TotalSalesToday = totalSalesToday,
                TotalSalesMonth = totalSalesMonth,
                TotalSoldProducts = totalSoldProducts,
                CancelledOrdersCount = totalCancelledOrders,
                BestSellingProduct = bestSelling?.Name ?? "N/A",
                BestSellingQuantity = bestSelling?.Qty ?? 0,
                WorstSellingProduct = worstSelling?.Name ?? "N/A",
                WorstSellingQuantity = worstSelling?.Qty ?? 0,
                LatestMessages = messages,
                LatestFeedbacks = feedbacks
            };
        }

        public async Task<IEnumerable<OwnerOrderDto>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OwnerOrderDto
                {
                    OrderID = o.OrderID,
                    UserName = o.User.FirstName,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.Payment.PaymentMethod,
                    Status = o.Status,
                    OrderDate = o.OrderDate,
                    Items = o.OrderItems.Select(i => new OrderItemDetailsDto
                    {
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                }).ToListAsync();
        }

        public async Task<bool> DeleteMessage(int messageId)
        {
            var message = await _context.ContactMessages.FindAsync(messageId);
            if (message == null) return false;

            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetProofImage(int orderId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderID == orderId);

            return payment?.ProofImagePath;
        }
    }
}
