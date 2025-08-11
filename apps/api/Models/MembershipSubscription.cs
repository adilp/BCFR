using System.ComponentModel.DataAnnotations;

namespace MemberOrgApi.Models
{
    public class MembershipSubscription
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string MembershipTier { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string StripeCustomerId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string StripeSubscriptionId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, cancelled, past_due, etc.
        
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime NextBillingDate { get; set; }
        public decimal Amount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}