using Microsoft.AspNetCore.Mvc;
using ElectionShield.Services;
using ElectionShield.ViewModels;
using System.Threading.Tasks;
using ElectionShield.Data;
using ElectionShield.Models;

namespace ElectionShield.Controllers
{
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;
        private readonly AiService _aiService;
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context, IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
            _context = context;
            //_aiService = new AiService();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportViewModel model, string createdBy)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.CreatedBy = HttpContext.Session.GetString("UserID");
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

        [HttpGet]
        public async Task<IActionResult> GetVerifiedReports(int id)
        {
            return View();
        }

        [HttpPost]

        public async Task<IActionResult> GetVerifiedReports()
        {
            var reports = await _reportService.GetVerifiedReportsAsync();
            return Ok(reports);
        }

        [HttpPost]
        public async Task<IActionResult> UploadReport()
        {
           var file = Request.Form.Files[0];
           if (file.Length > 0)
           {
               var filePath = Path.Combine("wwwroot/uploads", file.FileName);
               using (var stream = new FileStream(filePath, FileMode.Create))
               {
                   await file.CopyToAsync(stream);
               }
                var aiResultJson = await _aiService.AnalyzeFileAsync(filePath);
                var report = new Report
                {
                };
                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true, aiResult = aiResultJson });
            }
            return Json(new { success = false });
        }



    }
}