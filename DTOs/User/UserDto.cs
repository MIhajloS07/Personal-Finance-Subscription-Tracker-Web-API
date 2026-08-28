using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Model;
using System.ComponentModel.DataAnnotations;

namespace Personal_Finance___Subscription_Tracker_API.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<SubscriptionDto> Subscriptions { get; set; } = new();
    }
}
