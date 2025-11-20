using System.ComponentModel.DataAnnotations;

namespace ElectionShield.ViewModels
{
    public class TrackReportViewModel
    {
        [Required(ErrorMessage = "Report code is required")]
        [StringLength(10, ErrorMessage = "Report code must be 10 characters")]
        [Display(Name = "Report Tracking Code")]
        public string ReportCode { get; set; } = string.Empty;
    }

    public class TrackReportResultViewModel
    {
        public bool Found { get; set; }
        public string? Message { get; set; }
        public ReportViewModel? Report { get; set; }
        public string? ReportCode { get; set; }
    }
}