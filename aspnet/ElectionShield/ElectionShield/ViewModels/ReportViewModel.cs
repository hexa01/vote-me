using ElectionShield.Models;

namespace ElectionShield.ViewModels
{
    public class ReportViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ReportCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsAnonymous { get; set; }
        public ReportStatus Status { get; set; }
        public ReportPriority Priority { get; set; }
        public int ViewCount { get; set; }

        public List<MediaFileViewModel> MediaFiles { get; set; } = new();
        public AdminVerificationViewModel? Verification { get; set; }
        public AIAnalysisViewModel? AIAnalysis { get; set; }

        public string? AiTag { get; set; }

        // Helper properties
        public string StatusBadgeClass => Status switch
        {
            ReportStatus.Pending => "bg-secondary",
            ReportStatus.UnderReview => "bg-info",
            ReportStatus.Verified => "bg-success",
            ReportStatus.Rejected => "bg-danger",
            ReportStatus.Resolved => "bg-primary",
            ReportStatus.Escalated => "bg-warning",
            ReportStatus.RejectedByAI => "bg-danger",
            _ => "bg-secondary"
        };

        public string PriorityBadgeClass => Priority switch
        {
            ReportPriority.Low => "bg-success",
            ReportPriority.Medium => "bg-warning",
            ReportPriority.High => "bg-danger",
            ReportPriority.Critical => "bg-dark",
            _ => "bg-secondary"
        };

        public string CreatedAtFormatted => CreatedAt.ToString("MMM dd, yyyy h:mm tt");
        public string TimeAgo => GetTimeAgo(CreatedAt);

        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;

            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";

            return "just now";
        }
    }

    public class MediaFileViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public MediaType Type { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsProcessed { get; set; }

        // Helper properties
        public string FileSizeFormatted => FormatFileSize(FileSize);
        public bool IsImage => Type == MediaType.Image;
        public bool IsVideo => Type == MediaType.Video;
        public bool IsDocument => Type == MediaType.Document;

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class AdminVerificationViewModel
    {
        public Guid Id { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public string AdminUserFullName { get; set; } = string.Empty;
        public VerificationStatus Status { get; set; }
        public string? Comments { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string? ActionTaken { get; set; }
        public string? EscalatedTo { get; set; }
        public DateTime? FollowUpDate { get; set; }

        public string StatusBadgeClass => Status switch
        {
            VerificationStatus.Pending => "bg-secondary",
            VerificationStatus.Approved => "bg-success",
            VerificationStatus.Rejected => "bg-danger",
            VerificationStatus.NeedsMoreInfo => "bg-warning",
            VerificationStatus.Escalated => "bg-info",
            VerificationStatus.Duplicate => "bg-dark",
            VerificationStatus.RejectedByAI => "bg-danger",
            _ => "bg-secondary"
        };

        public string VerifiedAtFormatted => VerifiedAt.ToString("MMM dd, yyyy h:mm tt");
    }

    public class AIAnalysisViewModel
    {
        public Guid Id { get; set; }
        public string AnalysisType { get; set; } = string.Empty;
        public string? AnalysisResult { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public AIAnalysisStatus Status { get; set; }
        public string? Flags { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public string? AIProvider { get; set; }
        public string? ModelVersion { get; set; }

        public string StatusBadgeClass => Status switch
        {
            AIAnalysisStatus.Pending => "bg-secondary",
            AIAnalysisStatus.Processing => "bg-info",
            AIAnalysisStatus.Completed => "bg-success",
            AIAnalysisStatus.Failed => "bg-danger",
            AIAnalysisStatus.Skipped => "bg-warning",
            _ => "bg-secondary"
        };

        public string ConfidenceScoreFormatted => ConfidenceScore.HasValue ? $"{ConfidenceScore.Value:P1}" : "N/A";
        public string AnalyzedAtFormatted => AnalyzedAt.ToString("MMM dd, yyyy h:mm tt");
    }
}