using System.ComponentModel.DataAnnotations;

namespace Personal_Finance___Subscription_Tracker_API.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        // Subscription relation
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
