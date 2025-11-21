using ElectionShield.Data;
using ElectionShield.Models;
using ElectionShield.ViewModels;
using MailKit;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace ElectionShield.Services
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(CreateReportViewModel model);
        Task<ReportViewModel?> GetReportByCodeAsync(string reportCode);
        Task<List<ReportViewModel>> GetApprovedReportsAsync();
        Task<List<ReportViewModel>> GetAllReportsAsync();
        Task<List<ReportViewModel>> GetVerifiedReportsAsync();
        Task<List<ReportViewModel>> GetPendingReportsAsync();
        Task<List<ReportViewModel>> GetReportsByStatusAsync(ReportStatus status);
        Task<bool> UpdateReportStatusAsync(Guid reportId, ReportStatus status);
        Task<bool> UpdateReportPriorityAsync(Guid reportId, ReportPriority priority);
        Task<string> GenerateReportCodeAsync();
        Task<int> GetReportsCountAsync();
        Task<int> GetReportsCountByStatusAsync(ReportStatus status);
        Task<bool> DeleteReportAsync(Guid reportId);
        Task<Report?> GetReportByIdAsync(Guid reportId);
    }

    public class ReportService : IReportService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly AiService _aiService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
     ApplicationDbContext context,
     IFileService fileService,
     ILogger<ReportService> logger,
     AiService aiService,
     IWebHostEnvironment environment
 )
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
            _aiService = aiService;
            _environment = environment;
        }

        public async Task<Report> CreateReportAsync(CreateReportViewModel model)
        {
            try
            {
                var report = new Report
                {
                    Title = model.Title,
                    Description = model.Description,
                    Location = model.Location,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Category = model.Category,
                    IsAnonymous = model.IsAnonymous,
                    ReporterEmail = model.ReporterEmail,
                    ReporterPhone = model.ReporterPhone,
                    ReportCode = await GenerateReportCodeAsync(),
                    Status = ReportStatus.Pending,
                    Priority = ReportPriority.Medium,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = model.CreatedBy
                };
                
                // var fileUpload = await ElectionShield.Services.AnalyzeFileAsync();
                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                string? firstSavedFilePath = null;

                if (model.MediaFiles != null && model.MediaFiles.Any())
                {
                    foreach (var file in model.MediaFiles)
                    {
                        if (file.Length > 0)
                        {
                            try
                            {
                                var filePath = await _fileService.SaveFileAsync(file, "reports");
                                if (firstSavedFilePath == null)
                                    firstSavedFilePath = filePath;

                                var mediaFile = new MediaFile
                                {
                                    FileName = file.FileName,
                                    FilePath = filePath,
                                    ContentType = _fileService.GetContentType(file.FileName),
                                    FileSize = file.Length,
                                    Type = GetMediaType(file.ContentType),
                                    ReportId = report.Id,
                                    UploadedAt = DateTime.UtcNow
                                };

                                _context.MediaFiles.Add(mediaFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error saving media file for report {ReportId}", report.Id);
                            }
                        }
                    }

                    await _context.SaveChangesAsync(); 
                }

                if (firstSavedFilePath != null)
                {
                    _logger.LogInformation("firstSavedFilePath: {Path}", firstSavedFilePath);
                    // _logger.LogInformation("_aiService is null? {Value}", _aiService == null);
                    _logger.LogInformation("_fileService is null? {Value}", _fileService == null);
                    _logger.LogInformation("_context is null? {Value}", _context == null);

                    var absoluteFilePath = Path.Combine(_environment.WebRootPath, firstSavedFilePath);

                    var analysis = await _aiService.AnalyzeFileAsync(absoluteFilePath);

                    report.AiAnalysisResult = analysis;
                    string result = report.AiAnalysisResult;
                    using JsonDocument doc = JsonDocument.Parse(result);
                    JsonElement root = doc.RootElement;

                    double riskScore = root.GetProperty("risk_score").GetDouble();
                    if(riskScore > 5.0)
                    {
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        report.Status = ReportStatus.RejectedByAI;
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Report created successfully: {ReportCode}", report.ReportCode);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }

        private string ToAbsolutePath(string relativePath)
        {
            return Path.Combine(_environment.WebRootPath, relativePath.Replace("/", "\\"));
        }



        public async Task<ReportViewModel?> GetReportByCodeAsync(string reportCode)
        {
            try
            {
                var report = await _context.Reports
                    .Include(r => r.MediaFiles)
                    .Include(r => r.Verification)
                    .ThenInclude(v => v.AdminUser)
                    .Include(r => r.AIAnalysis)
                    .FirstOrDefaultAsync(r => r.ReportCode == reportCode.ToUpper());

                if (report == null)
                    return null;

                // Increment view count
                report.ViewCount++;
                await _context.SaveChangesAsync();

                return MapToViewModel(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report by code: {ReportCode}", reportCode);
                throw;
            }
        }

        public async Task<List<ReportViewModel>> GetAllReportsAsync()
        {
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.MediaFiles)
                    .Include(r => r.Verification)
                    .ThenInclude(v => v.AdminUser)
                    // .Include(r => r.AIAnalysis)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return reports.Select(MapToViewModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all reports");
                throw;
            }
        }
        public async Task<List<ReportViewModel>> GetApprovedReportsAsync()
        {
            return await _context.Reports
                .Where(r => r.Status == ReportStatus.Verified) 
                .Include(r => r.MediaFiles)
                .Select(r => new ReportViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Description = r.Description,
                    Category = r.Category,
                    Location = r.Location,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Status = r.Status,
                    Priority = r.Priority,
                    CreatedAt = r.CreatedAt,
                    MediaFiles = r.MediaFiles.Select(m => new MediaFileViewModel
                    {
                        Id = m.Id,
                        FilePath = m.FilePath,
                        FileName = m.FileName
                    }).ToList()
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        public async Task<List<ReportViewModel>> GetVerifiedReportsAsync()
        {
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.MediaFiles)
                    .Include(r => r.Verification)
                    .ThenInclude(v => v.AdminUser)
                    // .Include(r => r.AIAnalysis)
                    .OrderByDescending(r => r.CreatedAt)
                    .Where(r => r.Verification.Status.ToString() == "Verified")
                    .ToListAsync();

                return reports.Select(MapToViewModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all reports");
                throw;
            }
        }


        public async Task<List<ReportViewModel>> GetPendingReportsAsync()
        {
            return await GetReportsByStatusAsync(ReportStatus.Pending);
        }

        public async Task<List<ReportViewModel>> GetReportsByStatusAsync(ReportStatus status)
        {
            try
            {
                var reports = await _context.Reports
                    .Where(r => r.Status == status)
                    .Include(r => r.MediaFiles)
                    .Include(r => r.Verification)
                    .ThenInclude(v => v.AdminUser)
                    .Include(r => r.AIAnalysis)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return reports.Select(MapToViewModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports by status: {Status}", status);
                throw;
            }
        }

        public async Task<bool> UpdateReportStatusAsync(Guid reportId, ReportStatus status)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                    return false;

                report.Status = status;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Report status updated: {ReportId} -> {Status}", reportId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report status: {ReportId}", reportId);
                return false;
            }
        }

        public async Task<bool> UpdateReportPriorityAsync(Guid reportId, ReportPriority priority)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                    return false;

                report.Priority = priority;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Report priority updated: {ReportId} -> {Priority}", reportId, priority);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report priority: {ReportId}", reportId);
                return false;
            }
        }

        public async Task<string> GenerateReportCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;

            do
            {
                code = new string(Enumerable.Repeat(chars, 10)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (await _context.Reports.AnyAsync(r => r.ReportCode == code));

            return code;
        }

        public async Task<int> GetReportsCountAsync()
        {
            return await _context.Reports.CountAsync();
        }

        public async Task<int> GetReportsCountByStatusAsync(ReportStatus status)
        {
            return await _context.Reports.CountAsync(r => r.Status == status);
        }

        public async Task<bool> DeleteReportAsync(Guid reportId)
        {
            try
            {
                var report = await _context.Reports
                    .Include(r => r.MediaFiles)
                    .Include(r => r.Verification)
                    .Include(r => r.AIAnalysis)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null)
                    return false;

                // Delete associated media files
                foreach (var mediaFile in report.MediaFiles)
                {
                    await _fileService.DeleteFileAsync(mediaFile.FilePath);
                }

                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Report deleted successfully: {ReportId}", reportId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report: {ReportId}", reportId);
                return false;
            }
        }

        public async Task<Report?> GetReportByIdAsync(Guid reportId)
        {
            return await _context.Reports
                .Include(r => r.MediaFiles)
                .Include(r => r.Verification)
                .ThenInclude(v => v.AdminUser)
                .Include(r => r.AIAnalysis)
                .FirstOrDefaultAsync(r => r.Id == reportId);
        }

        private ReportViewModel MapToViewModel(Report report)
        {
            return new ReportViewModel
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                Location = report.Location,
                Latitude = report.Latitude,
                Longitude = report.Longitude,
                Category = report.Category,
                ReportCode = report.ReportCode,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt,
                IsAnonymous = report.IsAnonymous,
                Status = report.Status,
                Priority = report.Priority,
                ViewCount = report.ViewCount,
                MediaFiles = report.MediaFiles.Select(m => new MediaFileViewModel
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    FilePath = m.FilePath,
                    ContentType = m.ContentType,
                    FileSize = m.FileSize,
                    Type = m.Type,
                    UploadedAt = m.UploadedAt,
                    IsProcessed = m.IsProcessed
                }).ToList(),
                Verification = report.Verification != null ? new AdminVerificationViewModel
                {
                    Id = report.Verification.Id,
                    AdminUserName = report.Verification.AdminUser?.UserName ?? "Unknown",
                    AdminUserFullName = report.Verification.AdminUser?.FullName ?? "Unknown Admin",
                    Status = report.Verification.Status,
                    Comments = report.Verification.Comments,
                    VerifiedAt = report.Verification.VerifiedAt,
                    ActionTaken = report.Verification.ActionTaken,
                    EscalatedTo = report.Verification.EscalatedTo,
                    FollowUpDate = report.Verification.FollowUpDate
                } : null,
                AIAnalysis = report.AIAnalysis != null ? new AIAnalysisViewModel
                {
                    Id = report.AIAnalysis.Id,
                    AnalysisType = report.AIAnalysis.AnalysisType,
                    AnalysisResult = report.AIAnalysis.AnalysisResult,
                    ConfidenceScore = report.AIAnalysis.ConfidenceScore,
                    Status = report.AIAnalysis.Status,
                    Flags = report.AIAnalysis.Flags,
                    AnalyzedAt = report.AIAnalysis.AnalyzedAt,
                    AIProvider = report.AIAnalysis.AIProvider,
                    ModelVersion = report.AIAnalysis.ModelVersion
                } : null
            };
        }

        private MediaType GetMediaType(string contentType)
        {
            if (contentType.StartsWith("image/"))
                return MediaType.Image;
            else if (contentType.StartsWith("video/"))
                return MediaType.Video;
            else if (contentType.StartsWith("audio/"))
                return MediaType.Audio;
            else
                return MediaType.Document;
        }
    }
}