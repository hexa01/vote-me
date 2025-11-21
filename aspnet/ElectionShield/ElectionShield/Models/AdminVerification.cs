using System.ComponentModel.DataAnnotations;

namespace ElectionShield.Models
{
    public class AdminVerification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReportId { get; set; }

        [Required]
        public string AdminUserId { get; set; } = string.Empty;

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        [StringLength(1000)]
        public string? Comments { get; set; }

        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string? ActionTaken { get; set; }
        public string? EscalatedTo { get; set; }
        public DateTime? FollowUpDate { get; set; }

        // Navigation properties
        public virtual Report Report { get; set; } = null!;
        public virtual ApplicationUser AdminUser { get; set; } = null!;
    }

    public enum VerificationStatus
    {
        Pending,
        Approved,
        Rejected,
        NeedsMoreInfo,
        Escalated,
        Duplicate,

        RejectedByAI
    }
}