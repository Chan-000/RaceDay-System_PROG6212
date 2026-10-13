using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Users
    {
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength (50)]
        public string LastName {  get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;//Organiser or Partcipant

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

    }
}
