using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    public class Feedback
    {
        public int FeedbackID { get; set; }

        public int ProductID { get; set; }
        public int UserID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public Product Product { get; set; }
        public User User { get; set; }
    }
}
