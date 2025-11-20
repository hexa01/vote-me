using ElectionShield.Models;

namespace ElectionShield.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int VerifiedReports { get; set; }
        public int RejectedReports { get; set; }
        public int ResolvedReports { get; set; }

        public int TodayReports { get; set; }
        public int WeekReports { get; set; }
        public int MonthReports { get; set; }

        public List<ReportViewModel> RecentReports { get; set; } = new();
        public List<CategoryStats> CategoryStatistics { get; set; } = new();
        public List<StatusStats> StatusStatistics { get; set; } = new();

        public int HighPriorityReports { get; set; }
        public int CriticalPriorityReports { get; set; }

        public int TotalMediaFiles { get; set; }
        public int PendingAIAnalysis { get; set; }
    }

    public class CategoryStats
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class StatusStats
    {
        public ReportStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}