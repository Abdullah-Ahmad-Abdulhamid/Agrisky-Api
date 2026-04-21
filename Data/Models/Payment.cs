using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    /// <summary>
    /// Represents the state of a payment for an order.
    /// Status is ALWAYS set server-side; the client never controls it.
    /// </summary>
    public enum PaymentStatus
    {
        PendingVerification,   // VodafoneCash uploaded proof — awaiting admin review
        AwaitingPayment,       // Cash-on-delivery — payment due on receipt
        Paid,                  // Admin confirmed / gateway webhook confirmed
        Rejected,              // Admin rejected the uploaded proof
        Refunded               // Payment returned to customer
    }

    public class Payment
    {
        public int PaymentID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Explicitly required — no dangerous default of "Paid".
        /// Set server-side only via PaymentStatus enum.
        /// </summary>
        [Required]
        public string Status { get; set; } = PaymentStatus.PendingVerification.ToString();

        /// <summary>
        /// Optional reference from payment provider (transaction ID, webhook ref, etc.)
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Optional admin note when reviewing proof (rejection reason, etc.)
        /// </summary>
        public string? AdminNote { get; set; }

        public Order Order { get; set; } = null!;

        /// <summary>
        /// Path to uploaded proof image — stored relative to wwwroot/private/proofs/
        /// NOT served by static files middleware.
        /// </summary>
        public string? ProofImagePath { get; set; }
    }
}