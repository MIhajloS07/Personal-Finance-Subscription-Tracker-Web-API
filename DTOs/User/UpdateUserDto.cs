using System.ComponentModel.DataAnnotations;

namespace Personal_Finance___Subscription_Tracker_API.DTOs.User
{
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        // optional
        public string? NewPassword { get; set; } = null;
    }
}
