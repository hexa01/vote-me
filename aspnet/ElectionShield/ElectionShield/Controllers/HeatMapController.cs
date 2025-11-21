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
    public class HeatMapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HeatMapController> _logger;

        public HeatMapController(ApplicationDbContext context, ILogger<HeatMapController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var viewModel = new HeatMapViewModel
            {
                ActiveSection = "heatmap",
                Categories = await GetCategoriesAsync(),
                Statuses = Enum.GetValues<ReportStatus>().ToList(),
                Priorities = Enum.GetValues<ReportPriority>().ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetHeatMapData(
            string? category = null,
            ReportStatus? status = null,
            ReportPriority? priority = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var query = _context.Reports.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(category))
                    query = query.Where(r => r.Category == category);

                if (status.HasValue)
                    query = query.Where(r => r.Status == status.Value);

                if (priority.HasValue)
                    query = query.Where(r => r.Priority == priority.Value);

                if (startDate.HasValue)
                    query = query.Where(r => r.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(r => r.CreatedAt <= endDate.Value);

                var reports = await query
                    .Select(r => new HeatMapPoint
                    {
                        Latitude = r.Latitude,
                        Longitude = r.Longitude,
                        ReportId = r.Id,
                        Title = r.Title,
                        Category = r.Category,
                        Status = r.Status,
                        Priority = r.Priority,
                        CreatedAt = r.CreatedAt,
                        Location = r.Location
                        // Intensity = CalculateIntensity(r.Priority, r.Status)
                    })
                    .ToListAsync();

                // Group nearby points into clusters
                var clusteredData = ClusterPoints(reports, 0.01); // 0.01 degree ~ 1.1km

                return Ok(new
                {
                    success = true,
                    points = reports,
                    clusters = clusteredData,
                    totalPoints = reports.Count,
                    filteredBy = new
                    {
                        category,
                        status,
                        priority,
                        startDate,
                        endDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting heatmap data");
                return StatusCode(500, new { success = false, error = "Error loading heatmap data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationStats(string? category = null)
        {
            try
            {
                var query = _context.Reports.AsQueryable();

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(r => r.Category == category);

                var locationStats = await query
                    .GroupBy(r => new { r.Location, r.Latitude, r.Longitude })
                    .Select(g => new LocationStat
                    {
                        Location = g.Key.Location,
                        Latitude = g.Key.Latitude,
                        Longitude = g.Key.Longitude,
                        TotalReports = g.Count(),
                        VerifiedReports = g.Count(r => r.Status == ReportStatus.Verified),
                        HighPriorityReports = g.Count(r => r.Priority == ReportPriority.High || r.Priority == ReportPriority.Critical),
                        Categories = g.GroupBy(r => r.Category)
                            .Select(cg => new CategoryCount
                            {
                                Category = cg.Key,
                                Count = cg.Count()
                            })
                            .ToList()
                    })
                    .Where(ls => ls.TotalReports > 0)
                    .OrderByDescending(ls => ls.TotalReports)
                    .Take(50)
                    .ToListAsync();

                return Ok(new { success = true, locations = locationStats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location stats");
                return StatusCode(500, new { success = false, error = "Error loading location statistics" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIncidentHotspots()
        {
            try
            {
                var hotspots = await _context.Reports
                    .GroupBy(r => new { r.Latitude, r.Longitude, r.Location })
                    .Select(g => new  
                    {
                        Latitude = g.Key.Latitude,
                        Longitude = g.Key.Longitude,
                        Location = g.Key.Location,

                        ReportCount = g.Count(),
                        VerifiedCount = g.Count(r => r.Status == ReportStatus.Verified),
                        HighPriorityCount = g.Count(r => r.Priority == ReportPriority.High
                                                      || r.Priority == ReportPriority.Critical),

                        LatestIncident = g.Max(r => r.CreatedAt),

                        Categories = g.Select(r => r.Category)
                                      .Distinct()
                                      .ToList(),

                        RiskScore = g.Count() == 0 ? 0 :
                            (
                                g.Count(r => r.Priority == ReportPriority.Critical) * 1.0 +
                                g.Count(r => r.Priority == ReportPriority.High) * 0.8 +
                                g.Count(r => r.Priority == ReportPriority.Medium) * 0.5
                            ) / g.Count()
                    })
                    .Where(h => h.ReportCount >= 3)
                    .OrderByDescending(h => h.RiskScore)
                    .Take(20)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    hotspots
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident hotspots");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Error loading hotspots"
                });
            }
        }

        private async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Reports
                .Select(r => r.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        private double CalculateIntensity(ReportPriority priority, ReportStatus status)
        {
            double intensity = 0.5; // Base intensity

            // Increase intensity based on priority
            intensity += priority switch
            {
                ReportPriority.Low => 0.1,
                ReportPriority.Medium => 0.3,
                ReportPriority.High => 0.6,
                ReportPriority.Critical => 0.8,
                _ => 0
            };

            // Adjust based on status
            intensity += status switch
            {
                ReportStatus.Verified => 0.2,
                ReportStatus.Escalated => 0.3,
                _ => 0
            };

            return Math.Min(intensity, 1.0); // Cap at 1.0
        }

        private List<HeatMapCluster> ClusterPoints(List<HeatMapPoint> points, double clusterDistance)
        {
            var clusters = new List<HeatMapCluster>();
            var usedPoints = new HashSet<Guid>();

            foreach (var point in points.Where(p => !usedPoints.Contains(p.ReportId)))
            {
                var nearbyPoints = points.Where(p =>
                    !usedPoints.Contains(p.ReportId) &&
                    CalculateDistance(point.Latitude, point.Longitude, p.Latitude, p.Longitude) <= clusterDistance
                ).ToList();

                if (nearbyPoints.Count > 1) // Only cluster if there are multiple points
                {
                    var cluster = new HeatMapCluster
                    {
                        ClusterId = Guid.NewGuid(),
                        CenterLatitude = nearbyPoints.Average(p => p.Latitude),
                        CenterLongitude = nearbyPoints.Average(p => p.Longitude),
                        PointCount = nearbyPoints.Count,
                        AverageIntensity = nearbyPoints.Average(p => p.Intensity),
                        Points = nearbyPoints,
                        Categories = nearbyPoints.Select(p => p.Category).Distinct().ToList()
                    };

                    clusters.Add(cluster);

                    // Mark points as used
                    foreach (var usedPoint in nearbyPoints)
                    {
                        usedPoints.Add(usedPoint.ReportId);
                    }
                }
            }

            return clusters;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for distance calculation
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}