using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ElectionShield.Services;
using ElectionShield.ViewModels;
using ElectionShield.Data;
using ElectionShield.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectionShield.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IReportService reportService,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _reportService = reportService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var dashboardData = new AdminDashboardViewModel
                {
                    // Basic Statistics
                    TotalReports = await _context.Reports.CountAsync(),
                    PendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending),
                    VerifiedReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Verified),
                    RejectedReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Rejected),
                    RejectedByAIReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.RejectedByAI),

                    // Today's Activity
                    ReportsToday = await _context.Reports
                        .CountAsync(r => r.CreatedAt.Date == DateTime.UtcNow.Date),

                    // Priority Breakdown
                    HighPriorityReports = await _context.Reports
                        .CountAsync(r => r.Priority == ReportPriority.High || r.Priority == ReportPriority.Critical),

                    // Media Statistics
                    ReportsWithMedia = await _context.Reports
                        .CountAsync(r => r.MediaFiles.Any()),
                    TotalMediaFiles = await _context.MediaFiles.CountAsync(),

                    // Category Statistics
                    TopCategories = await _context.Reports
                        .GroupBy(r => r.Category)
                        .Select(g => new CategoryStat
                        {
                            Category = g.Key,
                            Count = g.Count(),
                            VerifiedCount = g.Count(r => r.Status == ReportStatus.Verified)
                        })
                        .OrderByDescending(c => c.Count)
                        .Take(5)
                        .ToListAsync(),

                    // Recent Reports
                    RecentReports = await _context.Reports
                        .Include(r => r.MediaFiles)
                        .Include(r => r.Verification)
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(10)
                        .Select(r => new ReportViewModel
                        {
                            Id = r.Id,
                            Title = r.Title,
                            ReportCode = r.ReportCode,
                            Status = r.Status,
                            Priority = r.Priority,
                            CreatedAt = r.CreatedAt,
                            Location = r.Location,
                            Category = r.Category,
                            MediaFiles = r.MediaFiles.Select(m => new MediaFileViewModel
                            {
                                FileName = m.FileName,
                                Type = m.Type
                            }).ToList(),
                            Verification = r.Verification != null ? new AdminVerificationViewModel
                            {
                                Status = r.Verification.Status,
                                VerifiedAt = r.Verification.VerifiedAt
                            } : null
                        })
                        .ToListAsync(),

                    // High Priority Alerts
                    HighPriorityAlerts = await _context.Reports
                        .Where(r => r.Priority == ReportPriority.High || r.Priority == ReportPriority.Critical)
                        .Where(r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.UnderReview)
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(5)
                        .Select(r => new ReportViewModel
                        {
                            Id = r.Id,
                            Title = r.Title,
                            ReportCode = r.ReportCode,
                            Status = r.Status,
                            Priority = r.Priority,
                            CreatedAt = r.CreatedAt,
                            Location = r.Location
                        })
                        .ToListAsync(),

                    // Active Sidebar Section
                    ActiveSection = "dashboard"
                };

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard data.";
                return View(new AdminDashboardViewModel { ActiveSection = "dashboard" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PendingReports()
        {
            var reports = await _reportService.GetPendingReportsAsync();
            var viewModel = new AdminListViewModel
            {
                Reports = reports,
                ActiveSection = "pending",
                PageTitle = "Pending Reports",
                TotalCount = reports.Count
            };
            return View("ReportList", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> AllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            var viewModel = new AdminListViewModel
            {
                Reports = reports,
                ActiveSection = "all",
                PageTitle = "All Reports",
                TotalCount = reports.Count
            };
            return View("ReportList", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> VerifiedReports()
        {
            var reports = await _context.Reports
                .Where(r => r.Status == ReportStatus.Verified)
                .Include(r => r.MediaFiles)
                .Include(r => r.Verification)
                .ThenInclude(v => v.AdminUser)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReportViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    ReportCode = r.ReportCode,
                    Status = r.Status,
                    Priority = r.Priority,
                    CreatedAt = r.CreatedAt,
                    Location = r.Location,
                    Category = r.Category,
                    Verification = r.Verification != null ? new AdminVerificationViewModel
                    {
                        Status = r.Verification.Status,
                        VerifiedAt = r.Verification.VerifiedAt,
                        AdminUserFullName = r.Verification.AdminUser.FullName ?? r.Verification.AdminUser.UserName
                    } : null
                })
                .ToListAsync();

            var viewModel = new AdminListViewModel
            {
                Reports = reports,
                ActiveSection = "verified",
                PageTitle = "Verified Reports",
                TotalCount = reports.Count
            };
            return View("ReportList", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            var analyticsData = new AnalyticsViewModel
            {
                ActiveSection = "analytics",
                // Category Distribution
                CategoryDistribution = await _context.Reports
                    .GroupBy(r => r.Category)
                    .Select(g => new CategoryStat
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        VerifiedCount = g.Count(r => r.Status == ReportStatus.Verified)
                    })
                    .OrderByDescending(c => c.Count)
                    .ToListAsync(),

                // Status Distribution
                StatusDistribution = await _context.Reports
                    .GroupBy(r => r.Status)
                    .Select(g => new StatusStat
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync(),

                // Daily Reports (Last 30 days)
                DailyReports = await _context.Reports
                    .Where(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(r => r.CreatedAt.Date)
                    .Select(g => new DailyReportStat
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToListAsync(),

                // Priority Distribution
                PriorityDistribution = await _context.Reports
                    .GroupBy(r => r.Priority)
                    .Select(g => new PriorityStat
                    {
                        Priority = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync()
            };

            return View(analyticsData);
        }

        [HttpGet]
        public async Task<IActionResult> ReportDetails(Guid id)
        {
            var report = await _context.Reports
                .Include(r => r.MediaFiles)
                .Include(r => r.Verification)
                .ThenInclude(v => v.AdminUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction("Dashboard");
            }

            var viewModel = new ReportDetailViewModel
            {
                Report = report,
                ActiveSection = "details"
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyReport(Guid reportId, VerificationStatus status, string? comments)
        {
            try
            {
                var verification = new AdminVerification
                {
                    Id = Guid.NewGuid(),
                    ReportId = reportId,
                    AdminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!,
                    Status = status,
                    Comments = comments,
                    VerifiedAt = DateTime.UtcNow
                };

                // _context.AdminVerifications.Add(verification);

                var report = await _context.Reports.FindAsync(reportId);
                if (report != null)
                {
                    report.Status = status == VerificationStatus.Approved ? ReportStatus.Verified : ReportStatus.Rejected;
                    report.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Report verification completed successfully!";
                return RedirectToAction("PendingReports");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying report");
                TempData["ErrorMessage"] = "An error occurred while verifying the report.";
                return RedirectToAction("ReportDetails", new { id = reportId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReportStatus(Guid reportId, ReportStatus status)
        {
            var result = await _reportService.UpdateReportStatusAsync(reportId, status);
            if (result)
            {
                TempData["SuccessMessage"] = "Report status updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update report status.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReportPriority(Guid reportId, ReportPriority priority)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                {
                    TempData["ErrorMessage"] = "Report not found.";
                    return RedirectToAction("Dashboard");
                }

                report.Priority = priority;
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Report priority updated successfully!";
                return RedirectToAction("ReportDetails", new { id = reportId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report priority");
                TempData["ErrorMessage"] = "Failed to update report priority.";
                return RedirectToAction("ReportDetails", new { id = reportId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchReports(string query, string category, ReportStatus? status)
        {
            var reportsQuery = _context.Reports.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                reportsQuery = reportsQuery.Where(r =>
                    r.Title.Contains(query) ||
                    r.Description.Contains(query) ||
                    r.Location.Contains(query) ||
                    r.ReportCode.Contains(query));
            }

            if (!string.IsNullOrEmpty(category))
            {
                reportsQuery = reportsQuery.Where(r => r.Category == category);
            }

            if (status.HasValue)
            {
                reportsQuery = reportsQuery.Where(r => r.Status == status.Value);
            }

            var reports = await reportsQuery
                .Include(r => r.MediaFiles)
                .Include(r => r.Verification)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReportViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    ReportCode = r.ReportCode,
                    Status = r.Status,
                    Priority = r.Priority,
                    CreatedAt = r.CreatedAt,
                    Location = r.Location,
                    Category = r.Category
                })
                .ToListAsync();

            var viewModel = new AdminListViewModel
            {
                Reports = reports,
                ActiveSection = "search",
                PageTitle = "Search Results",
                TotalCount = reports.Count,
                SearchQuery = query,
                SearchCategory = category,
                SearchStatus = status
            };

            return View("ReportList", viewModel);
        }
    }
}