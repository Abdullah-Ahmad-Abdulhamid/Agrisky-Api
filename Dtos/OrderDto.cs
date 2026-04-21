using System.ComponentModel.DataAnnotations;

namespace AgriskyApi.Dtos
{
    // ── Inbound: what the client sends per order item ─────────────────────────
    public class OrderItemInputDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive integer.")]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; }
    }

    // ── Inbound: shipping address supplied by the client ──────────────────────
    public class ShippingDto
    {
        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "ZipCode is required.")]
        [RegularExpression(@"^\d{4,10}$", ErrorMessage = "ZipCode must be 4-10 digits.")]
        public string ZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "PhoneNumber is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    // ── Inbound: multipart/form-data received from client ─────────────────────
    /// <summary>
    /// UserId is intentionally ABSENT — it is always read from the JWT claim.
    /// Items and Shipping arrive as JSON strings in a multipart/form-data envelope.
    /// </summary>
    public class CreateOrderFormInput
    {
        // UserId is NOT here — server derives it from HttpContext.User

        [Required(ErrorMessage = "PaymentMethod is required.")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Optional transaction ID for VodafoneCash payments.
        /// Must be 10-20 digits when PaymentMethod == "VodafoneCash".
        /// </summary>
        [StringLength(50)]
        public string? VodafoneCashTransactionId { get; set; }

        /// <summary>JSON array: [{"ProductId": 1, "Quantity": 2}]</summary>
        [Required(ErrorMessage = "Items JSON is required.")]
        public string Items { get; set; } = string.Empty;

        /// <summary>JSON object: {"Address":"...","City":"...","Country":"...","ZipCode":"...","PhoneNumber":"..."}</summary>
        [Required(ErrorMessage = "Shipping JSON is required.")]
        public string Shipping { get; set; } = string.Empty;
    }

    // ── Internal: passed from controller → repository (never from client) ─────
    public class CreateOrderDto
    {
        /// <summary>Always set by the controller from JWT — never from the client body.</summary>
        public int UserId { get; set; }

        public List<OrderItemInputDto> Items { get; set; } = new();
        public ShippingDto Shipping { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
        public string? VodafoneCashTransactionId { get; set; }
    }

    // ── Outbound: what users see about their own orders ───────────────────────
    public class OrderDto
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public List<OrderItemSummaryDto> Items { get; set; } = new();
    }

    public class OrderItemSummaryDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // ── Admin: status update request ──────────────────────────────────────────
    public class UpdateOrderStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
    }

    // ── Admin: payment verification request ───────────────────────────────────
    public class VerifyPaymentRequest
    {
        [Required]
        public bool Approve { get; set; }

        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
}