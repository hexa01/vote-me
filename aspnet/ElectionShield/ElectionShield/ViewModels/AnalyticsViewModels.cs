namespace ElectionShield.ViewModels
{
    public class DashboardAnalytics
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int VerifiedReports { get; set; }
        public int HighPriorityReports { get; set; }
    }

    public class FraudAnalytics { }
    public class GeographicAnalytics { }
    public class TemporalAnalytics { }
    public class CategoryAnalytics { }
    public class VerificationAnalytics { }
    public class ReportTrend { }
}