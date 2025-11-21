using System.ComponentModel.DataAnnotations;

namespace ElectionShield.Models
{
    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = "Other";

        [Required]
        [StringLength(10)]
        public string ReportCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsAnonymous { get; set; } = true;
        public string? ReporterEmail { get; set; }
        public string? ReporterPhone { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public ReportPriority Priority { get; set; } = ReportPriority.Medium;

        public int ViewCount { get; set; } = 0;
        public string? InternalNotes { get; set; }

        // Navigation properties
        public virtual ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
        public virtual AdminVerification? Verification { get; set; }
        public virtual AIAnalysis? AIAnalysis { get; set; }
        public string? AiAnalysisResult { get; set; }

        public string? CreatedBy {get; set;}
    }

    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Verified,
        Rejected,
        Resolved,
        Escalated,

        RejectedByAI
    }

    public enum ReportPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}