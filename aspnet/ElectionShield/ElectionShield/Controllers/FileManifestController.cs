using Microsoft.AspNetCore.Mvc;
using ElectionShield.Services;
using ElectionShield.ViewModels;
using System.Diagnostics;
using ElectionShield.Models;

namespace ElectionShield.Controllers
{
    public class FileManifestContoller : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}