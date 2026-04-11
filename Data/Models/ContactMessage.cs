using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class ContactMessage
    {

        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(1000)]
        public string Message { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public bool IsSeenByAdmin { get; set; } = false;

    }
}
