using ElectionShield.Models;

namespace ElectionShield.ViewModels
{
    public class HeatMapViewModel
    {
        public string ActiveSection { get; set; } = "heatmap";
        public List<string> Categories { get; set; } = new();
        public List<ReportStatus> Statuses { get; set; } = new();
        public List<ReportPriority> Priorities { get; set; } = new();
        public DateTime DefaultStartDate => DateTime.UtcNow.AddDays(-30);
        public DateTime DefaultEndDate => DateTime.UtcNow;
    }

    public class HeatMapPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid ReportId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public ReportPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Intensity { get; set; }
    }

    public class HeatMapCluster
    {
        public Guid ClusterId { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public int PointCount { get; set; }
        public double AverageIntensity { get; set; }
        public List<HeatMapPoint> Points { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    public class LocationStat
    {
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int TotalReports { get; set; }
        public int VerifiedReports { get; set; }
        public int HighPriorityReports { get; set; }
        public List<CategoryCount> Categories { get; set; } = new();
        public double VerificationRate => TotalReports > 0 ? (double)VerifiedReports / TotalReports * 100 : 0;
    }

    public class CategoryCount
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class Hotspot
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Location { get; set; } = string.Empty;
        public int ReportCount { get; set; }
        public int VerifiedCount { get; set; }
        public int HighPriorityCount { get; set; }
        public DateTime LatestIncident { get; set; }
        public List<string> Categories { get; set; } = new();
        public double RiskScore => CalculateRiskScore();

        private double CalculateRiskScore()
        {
            double score = ReportCount * 0.3;
            score += HighPriorityCount * 0.5;
            score += (DateTime.UtcNow - LatestIncident).TotalDays < 1 ? 0.2 : 0;
            return Math.Min(score / 10.0, 1.0);
        }
    }
}