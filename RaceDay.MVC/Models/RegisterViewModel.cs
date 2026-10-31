using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Name { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(8)]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";

        public string? OrganisationName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? EmergencyContact { get; set; }
    }
}
