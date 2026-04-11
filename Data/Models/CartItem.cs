using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}