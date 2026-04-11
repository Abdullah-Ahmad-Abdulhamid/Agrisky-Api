using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }


        [Required]
        public int ProductID { get; set; }

        [Required]
        public string ImageURL { get; set; }

        [StringLength(150)]
        public string? AltText { get; set; }

        public Product Product { get; set; }
    }
}
