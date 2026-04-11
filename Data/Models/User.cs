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

            [Required]
            public string PasswordHash { get; set; }

            [Phone, MaxLength(15)]
            public string? PhoneNumber { get; set; }

            [MaxLength(200)]
            public string? Address { get; set; }

            [Required]
            public string Role { get; set; } = "User";

            public Shipping? Shipping { get; set; }

            public ICollection<Order> Orders { get; set; } = new List<Order>();
            public Cart? Cart { get; set; }
            public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        }
    }
