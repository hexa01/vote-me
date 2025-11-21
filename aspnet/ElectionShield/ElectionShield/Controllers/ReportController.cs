using Microsoft.AspNetCore.Mvc;
using ElectionShield.Services;
using ElectionShield.ViewModels;
using System.Threading.Tasks;

namespace ElectionShield.Controllers
{
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var report = await _reportService.CreateReportAsync(model);
                TempData["SuccessMessage"] = $"Report submitted successfully! Your tracking code is: {report.ReportCode}";
                return RedirectToAction("Success", new { code = report.ReportCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                ModelState.AddModelError("", "An error occurred while submitting your report. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Success(string code)
        {
            ViewBag.ReportCode = code;
            return View();
        }

        [HttpGet]
        public IActionResult Track()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(string reportCode)
        {
            if (string.IsNullOrWhiteSpace(reportCode))
            {
                ModelState.AddModelError("", "Please enter a report code");
                return View();
            }

            var report = await _reportService.GetReportByCodeAsync(reportCode.ToUpper());
            if (report == null)
            {
                ModelState.AddModelError("", "Report not found. Please check your code and try again.");
                return View();
            }

            return View("ReportDetails", report);
        }

        [HttpGet]
        public IActionResult ReportDetails(ReportViewModel model)
        {
            return View(model);
        }

        [HttpGet]

        public async Task<IActionResult> GetAllReports(int id)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return Ok(reports);
        }
    }
}