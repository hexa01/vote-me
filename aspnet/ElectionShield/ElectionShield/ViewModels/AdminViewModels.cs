using ElectionShield.Models;

namespace ElectionShield.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Basic Statistics
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int VerifiedReports { get; set; }
        public int RejectedReports { get; set; }
        public int ReportsToday { get; set; }
        public int HighPriorityReports { get; set; }
        public int ReportsWithMedia { get; set; }
        public int TotalMediaFiles { get; set; }

        // Categories
        public List<CategoryStat> TopCategories { get; set; } = new();

        // Recent Data
        public List<ReportViewModel> RecentReports { get; set; } = new();
        public List<ReportViewModel> HighPriorityAlerts { get; set; } = new();

        // Sidebar
        public string ActiveSection { get; set; } = "dashboard";
    }

    public class AdminListViewModel
    {
        public List<ReportViewModel> Reports { get; set; } = new();
        public string ActiveSection { get; set; } = "all";
        public string PageTitle { get; set; } = "Reports";
        public int TotalCount { get; set; }

        // Search
        public string? SearchQuery { get; set; }
        public string? SearchCategory { get; set; }
        public ReportStatus? SearchStatus { get; set; }
    }

    public class ReportDetailViewModel
    {
        public Report Report { get; set; } = null!;
        public string ActiveSection { get; set; } = "details";
    }

    public class AnalyticsViewModel
    {
        public string ActiveSection { get; set; } = "analytics";
        public List<CategoryStat> CategoryDistribution { get; set; } = new();
        public List<StatusStat> StatusDistribution { get; set; } = new();
        public List<DailyReportStat> DailyReports { get; set; } = new();
        public List<PriorityStat> PriorityDistribution { get; set; } = new();
    }

    // Supporting models
    public class CategoryStat
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public int VerifiedCount { get; set; }
        public double VerificationRate => Count > 0 ? (double)VerifiedCount / Count * 100 : 0;
    }

    public class StatusStat
    {
        public ReportStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class DailyReportStat
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class PriorityStat
    {
        public ReportPriority Priority { get; set; }
        public int Count { get; set; }
    }
}