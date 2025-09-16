using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemberOrgApi.Models
{
    [Table("EmailJobs", Schema = "memberorg")]
    public class EmailJob
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid CreatedBy { get; set; }
        
        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Subject { get; set; }
        
        [Required]
        public string Body { get; set; }
        
        public bool IsHtml { get; set; } = true;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = EmailJobStatus.Pending;
        
        public int TotalRecipients { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        
        public DateTime? ScheduledFor { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<EmailJobRecipient> Recipients { get; set; }
    }
    
    [Table("EmailJobRecipients", Schema = "memberorg")]
    public class EmailJobRecipient
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid JobId { get; set; }
        
        [ForeignKey("JobId")]
        public virtual EmailJob Job { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Email { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = EmailJobStatus.Pending;
        
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    [Table("EmailQuota", Schema = "memberorg")]
    public class EmailQuota
    {
        [Key]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }
        
        public int EmailsSent { get; set; }
        public int QuotaLimit { get; set; } = 100;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public static class EmailJobStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
        public const string Paused = "Paused";
    }
}