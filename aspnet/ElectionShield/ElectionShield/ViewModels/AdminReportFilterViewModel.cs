using ElectionShield.Models;

namespace ElectionShield.ViewModels
{
    public class AdminReportFilterViewModel
    {
        public string? Search { get; set; }
        public ReportStatus? Status { get; set; }
        public ReportPriority? Priority { get; set; }
        public string? Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? HasMedia { get; set; }
        public bool? IsVerified { get; set; }

        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminReportListViewModel
    {
        public List<ReportViewModel> Reports { get; set; } = new();
        public AdminReportFilterViewModel Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Filter.PageSize);
        public bool HasPrevious => Filter.Page > 1;
        public bool HasNext => Filter.Page < TotalPages;
    }
}