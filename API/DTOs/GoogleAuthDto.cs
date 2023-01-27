using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class GoogleAuthDto
    {
        [Required]
        public string DisplayName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string GoogleId { get; set; }
    }
}