using Agrisky.Models;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Agrisky.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        [Required]
        public decimal TotalAmount { get; set; }

        public User User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public int? ShippingID { get; set; }
        public Shipping Shipping { get; set; }
        public string PaymentMethod { get; set; }
        public Payment Payment { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsSeenByAdmin { get; set; } = false;
        public bool IsSeenByUser { get; set; } = true;
        public bool IsDeliveredSeen { get; set; } = true;
    }
}
