using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }

        public int OrderID { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        [Required]
        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Paid";

        public Order Order { get; set; }
        public string ProofImagePath { get; set; }

    }
}
