using ElectionShield.Models;

namespace ElectionShield.ViewModels
{
    public class ReportListViewModel
    {
        public List<ReportViewModel> Reports { get; set; } = new();
        public string ActiveSection { get; set; } = "all";
        public string PageTitle { get; set; } = "Reports";
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public string SortBy { get; set; } = "created";
        public string SortOrder { get; set; } = "desc";
        public string CategoryFilter { get; set; } = "";
        public ReportStatus? StatusFilter { get; set; }
        public List<string> AvailableCategories { get; set; } = new();

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public int ShowingFrom => TotalCount > 0 ? ((Page - 1) * PageSize) + 1 : 0;
        public int ShowingTo => Math.Min(Page * PageSize, TotalCount);
    }

    public class ReportActionsViewModel
    {
        public Report Report { get; set; } = null!;
        public string ActiveSection { get; set; } = "actions";
        public List<ReportAction> AvailableActions { get; set; } = new();
    }

    public class ReportAction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ButtonClass { get; set; } = "btn-primary";
        public string ActionUrl { get; set; } = string.Empty;
        public bool RequiresConfirmation { get; set; }
        public string ConfirmationMessage { get; set; } = string.Empty;
    }

    public class ReportExportRequest
    {
        public string Format { get; set; } = "csv";
        public string? Category { get; set; }
        public ReportStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeMediaInfo { get; set; }
        public bool IncludeVerificationDetails { get; set; }
    }

    public class ReportStatisticsViewModel
    {
        public string ActiveSection { get; set; } = "statistics";
        public int TotalReports { get; set; }
        public int ReportsLast24Hours { get; set; }
        public int ReportsLast7Days { get; set; }
        public List<StatusStat> StatusDistribution { get; set; } = new();
        public List<CategoryStat> CategoryDistribution { get; set; } = new();
        public List<PriorityStat> PriorityDistribution { get; set; } = new();
        public List<DailyReportStat> DailyReports { get; set; } = new();
        public List<LocationStat> TopLocations { get; set; } = new();
        public double AverageVerificationTime { get; set; }
        public double VerificationRate { get; set; }
    }

    public class ReportExportItem
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsAnonymous { get; set; }
        public string? ReporterEmail { get; set; }
        public string? ReporterPhone { get; set; }
        public int ViewCount { get; set; }
        public int MediaFileCount { get; set; }
        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerificationComments { get; set; }
    }
}