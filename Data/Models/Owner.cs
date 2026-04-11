namespace Agrisky.Models
{
    public class Owner
    {
        public int OwnerID { get; set; }
        public decimal TotalSalesToday { get; set; }
        public decimal TotalSalesMonth { get; set; }
        public int TotalSoldProducts { get; set; }
        public int CancelledOrdersCount { get; set; }

        public string BestSellingProduct { get; set; }
        public int BestSellingQuantity { get; set; }

        public string WorstSellingProduct { get; set; }
        public int WorstSellingQuantity { get; set; }

        public List<ContactMessage> LatestMessages { get; set; }
    }
}
