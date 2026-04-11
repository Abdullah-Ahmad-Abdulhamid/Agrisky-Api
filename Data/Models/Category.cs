using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Icon { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
