using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class Shipping
    {
        public int ShippingID { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Zip Code is required")]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string PhoneNumber { get; set; }

        public DateTime? ShippedDate { get; set; }

        public string DeliveryStatus { get; set; } = "Not Shipped";
        public ICollection<Order> Orders { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

    }
}
