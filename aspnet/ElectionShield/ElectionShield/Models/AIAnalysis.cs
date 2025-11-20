using System.ComponentModel.DataAnnotations;

namespace ElectionShield.Models
{
    public class AIAnalysis
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReportId { get; set; }

        [Required]
        [StringLength(50)]
        public string AnalysisType { get; set; } = "ContentAnalysis";

        public string? AnalysisResult { get; set; }
        public decimal? ConfidenceScore { get; set; }

        public AIAnalysisStatus Status { get; set; } = AIAnalysisStatus.Pending;

        [StringLength(1000)]
        public string? Flags { get; set; }

        public string? Metadata { get; set; }
        public string? ProcessedMediaIds { get; set; }

        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int ProcessingTimeMs { get; set; }
        public string? AIProvider { get; set; }
        public string? ModelVersion { get; set; }

        // Navigation properties
        public virtual Report Report { get; set; } = null!;
    }

    public enum AIAnalysisStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Skipped
    }
}