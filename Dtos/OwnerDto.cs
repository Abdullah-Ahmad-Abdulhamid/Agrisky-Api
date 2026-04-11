namespace AgriskyApi.Dtos
{
    public class DashboardDto
    {
        public decimal TotalSalesToday { get; set; }
        public decimal TotalSalesMonth { get; set; }
        public int TotalSoldProducts { get; set; }
        public int CancelledOrdersCount { get; set; }

        public string BestSellingProduct { get; set; }
        public int BestSellingQuantity { get; set; }

        public string WorstSellingProduct { get; set; }
        public int WorstSellingQuantity { get; set; }

        public List<ContactMessageDto> LatestMessages { get; set; }
        public List<FeedbackDto> LatestFeedbacks { get; set; }
    }
    public class ContactMessageDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
    public class FeedbackDto
    {
        public string UserName { get; set; }
        public string ProductName { get; set; }
        public string Comment { get; set; }
        public DateTime DateSubmitted { get; set; }
    }
    public class OwnerOrderDto
    {
        public int OrderID { get; set; }
        public string UserName { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }

        public List<OrderItemDetailsDto> Items { get; set; }
    }
    public class OrderItemDetailsDto
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

}
