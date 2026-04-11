using Agrisky.Models.Agrisky.Models;
using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public List<CartItem> CartItems { get; set; } = new();
    }
}