using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        [Required]
        public int UserID { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        public decimal TotalAmount { get; set; }

        // Audit fields — always server-set, never client-supplied
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }   // email of admin who last changed status

        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public int? ShippingID { get; set; }
        public Shipping? Shipping { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public Payment? Payment { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsSeenByAdmin { get; set; } = false;
        public bool IsSeenByUser { get; set; } = true;
        public bool IsDeliveredSeen { get; set; } = true;
    }
}