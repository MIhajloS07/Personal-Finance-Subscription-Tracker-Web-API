using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Personal_Finance___Subscription_Tracker_API.DTOs.Subscription
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
