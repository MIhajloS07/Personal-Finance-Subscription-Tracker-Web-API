using System.ComponentModel.DataAnnotations;

namespace Personal_Finance___Subscription_Tracker_API.DTOs.User
{
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string? NewPassword { get; set; }
    }
}
