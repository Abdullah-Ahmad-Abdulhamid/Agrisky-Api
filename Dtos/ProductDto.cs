using System.ComponentModel.DataAnnotations;

namespace AgriskyApi.Dtos
{
    // ── Outbound ──────────────────────────────────────────────────────────────
    public class ProductDto
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? CategoryName { get; set; }
        public List<string> Images { get; set; } = new();
    }

    // ── Inbound: Admin creates a product ─────────────────────────────────────
    public class CreateProductDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 99999, ErrorMessage = "Price must be between 0.01 and 99,999.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryID must be a valid positive integer.")]
        public int CategoryID { get; set; }
    }

    // ── Inbound: Admin updates a product ─────────────────────────────────────
    public class UpdateProductDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 99999, ErrorMessage = "Price must be between 0.01 and 99,999.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryID must be a valid positive integer.")]
        public int CategoryID { get; set; }
    }

    // ── Query parameters ──────────────────────────────────────────────────────
    public class ProductFilterParams
    {
        [MaxLength(100)]
        public string? Search { get; set; }

        public int? CategoryId { get; set; }
    }
}