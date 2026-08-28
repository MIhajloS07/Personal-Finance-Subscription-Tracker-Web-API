using System.ComponentModel.DataAnnotations;

namespace Personal_Finance___Subscription_Tracker_API.DTOs.Subscription
{
    public class UpdateSubscriptionDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(18)]
        public string Currency { get; set; } = "RSD";

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
    }
}
