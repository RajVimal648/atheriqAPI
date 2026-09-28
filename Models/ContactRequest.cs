using System.ComponentModel.DataAnnotations;

namespace atheriqAPI.Models
{
    public class ContactRequest
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Work email is required.")]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Company { get; set; }

        [StringLength(150)]
        public string? Service { get; set; }

        [Required(ErrorMessage = "Requirement is required.")]
        [StringLength(5000, ErrorMessage = "Message is too long (max 5000 characters).")]
        public string Message { get; set; } = string.Empty;

        public bool ConsentGiven { get; set; }

        // Honeypot: hidden field on the React form, must stay empty
        public string? Website { get; set; }
    }
}
