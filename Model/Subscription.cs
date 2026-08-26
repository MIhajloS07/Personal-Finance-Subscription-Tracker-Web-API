using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Personal_Finance___Subscription_Tracker_API.Model
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(18)]
        public string Currency { get; set; } = "RSD";

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        // Foreign key for user
        public int UserId { get; set; }
        public User User { get; set; }
    }
}