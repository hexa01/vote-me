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

        public AdminController(IReportService reportService, ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _reportService = reportService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> PendingReports()
        {
            var reports = await _reportService.GetPendingReportsAsync();
            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> ReportDetails(Guid id)
        {
            var report = await _context.Reports
                .Include(r => r.MediaFiles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                _context.AdminVerifications.Add(verification);

                var report = await _context.Reports.FindAsync(reportId);
                if (report != null)
                {
                    report.Status = status == VerificationStatus.Approved ? ReportStatus.Verified : ReportStatus.Rejected;
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
    }
}