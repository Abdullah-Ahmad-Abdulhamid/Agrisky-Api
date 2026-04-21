using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }

        public string? PasswordHash { get; set; }

        [Phone, MaxLength(15)]
        public string? PhoneNumber { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryDate { get; set; }
        [Required]
        public string Role { get; set; } = "User";

        // ── Google OAuth fields ───────────────────────────────────────
        public string? GoogleId { get; set; }
        public string? ProfilePicture { get; set; }

        // ── NEW FIELDS for UserService ─────────────────────────────────
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ── Navigation ────────────────────────────────────────────────
        public Shipping? Shipping { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public Cart? Cart { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}