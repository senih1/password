using System.ComponentModel.DataAnnotations;

namespace password.Models
{
    public class Register
    {
        [Required]
        public string FullName { get; set; } 
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string PasswordCheck { get; set; }
        [Required]
        public string Email { get; set; }
    }
}
