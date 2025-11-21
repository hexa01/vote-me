using Microsoft.AspNetCore.Mvc;
using ElectionShield.Services;
using ElectionShield.ViewModels;
using System.Diagnostics;
using ElectionShield.Models;

namespace ElectionShield.Controllers
{
    public class HomeController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IReportService reportService)
        {
            _logger = logger;
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Rules()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> UserPortal()
        {
            var reports = await _reportService.GetApprovedReportsAsync();
            return View(reports);
        }
    }
}