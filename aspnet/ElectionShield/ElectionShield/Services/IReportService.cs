using ElectionShield.Data;
using ElectionShield.Models;
using ElectionShield.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectionShield.Services
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(CreateReportViewModel model);
        Task<ReportViewModel?> GetReportByCodeAsync(string reportCode);
        Task<List<ReportViewModel>> GetAllReportsAsync();
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
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, IFileService fileService, ILogger<ReportService> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
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
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // Save media files
                if (model.MediaFiles != null && model.MediaFiles.Any())
                {
                    foreach (var file in model.MediaFiles)
                    {
                        if (file.Length > 0)
                        {
                            try
                            {
                                var filePath = await _fileService.SaveFileAsync(file, "reports");
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
                                // Continue with other files even if one fails
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
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
                    .Include(r => r.AIAnalysis)
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