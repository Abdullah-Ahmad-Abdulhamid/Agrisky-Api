using System.ComponentModel.DataAnnotations;

namespace Agrisky.Models
{
    using System.ComponentModel.DataAnnotations;

    namespace Agrisky.Models
    {
        public class LoginModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }
    }
}
