using System.ComponentModel.DataAnnotations;

namespace ElectionShield.Models
{
    public class MediaFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }
        public MediaType Type { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; } = false;
        public string? ProcessingResult { get; set; }

        // Foreign key
        public Guid ReportId { get; set; }

        // Navigation properties
        public virtual Report Report { get; set; } = null!;
    }

    public enum MediaType
    {
        Image,
        Video,
        Document,
        Audio
    }
}