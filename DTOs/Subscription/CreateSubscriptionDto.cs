using System.ComponentModel.DataAnnotations;

namespace Personal_Finance___Subscription_Tracker_API.DTOs.Subscription
{
    public class CreateSubscriptionDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be between 0.01 and 100000.00.")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(18, MinimumLength = 1, ErrorMessage = "Currency must be between 1 and 18 characters.")]
        public string Currency { get; set; } = "RSD";

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Category must be between 1 and 50 characters.")]
        public string Category { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }
    }
}
