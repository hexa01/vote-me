using Microsoft.AspNetCore.Http;

namespace ElectionShield.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);
        Task<bool> DeleteFileAsync(string filePath);
        string GetContentType(string fileName);
        Task<List<string>> SaveFilesAsync(List<IFormFile> files, string subDirectory);
        bool IsFileSizeValid(IFormFile file, long maxSize);
        bool IsFileTypeValid(IFormFile file, string[] allowedExtensions);
        string GetFileExtension(string fileName);
        long GetMaxFileSize();
        string[] GetAllowedImageExtensions();
        string[] GetAllowedVideoExtensions();
        string[] GetAllowedDocumentExtensions();
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        public FileService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<FileService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");

                // Validate file size
                if (!IsFileSizeValid(file, GetMaxFileSize()))
                    throw new InvalidOperationException($"File size exceeds the maximum allowed size of {GetMaxFileSize() / 1024 / 1024}MB");

                // Validate file type
                var extension = GetFileExtension(file.FileName);
                var allowedExtensions = GetAllowedExtensionsForType(GetContentType(file.FileName));
                if (!IsFileTypeValid(file, allowedExtensions))
                    throw new InvalidOperationException($"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", subDirectory);

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{extension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = Path.Combine("uploads", subDirectory, uniqueFileName).Replace("\\", "/");

                _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file?.FileName);
                throw;
            }
        }

        public async Task<List<string>> SaveFilesAsync(List<IFormFile> files, string subDirectory)
        {
            var savedPaths = new List<string>();

            if (files == null || !files.Any())
                return savedPaths;

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = await SaveFileAsync(file, subDirectory);
                    savedPaths.Add(filePath);
                }
            }

            return savedPaths;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }

                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public string GetContentType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            var extension = GetFileExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".webm" => "video/webm",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                _ => "application/octet-stream"
            };
        }

        public bool IsFileSizeValid(IFormFile file, long maxSize)
        {
            return file.Length <= maxSize;
        }

        public bool IsFileTypeValid(IFormFile file, string[] allowedExtensions)
        {
            var extension = GetFileExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant();
        }

        public long GetMaxFileSize()
        {
            return _configuration.GetValue<long>("FileUpload:MaxFileSize", 10485760); // 10MB default
        }

        public string[] GetAllowedImageExtensions()
        {
            return _configuration.GetSection("FileUpload:AllowedImageExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        }

        public string[] GetAllowedVideoExtensions()
        {
            return _configuration.GetSection("FileUpload:AllowedVideoExtensions").Get<string[]>()
                ?? new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv" };
        }

        public string[] GetAllowedDocumentExtensions()
        {
            return _configuration.GetSection("FileUpload:AllowedDocumentExtensions").Get<string[]>()
                ?? new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" };
        }

        private string[] GetAllowedExtensionsForType(string contentType)
        {
            if (contentType.StartsWith("image/"))
                return GetAllowedImageExtensions();
            else if (contentType.StartsWith("video/"))
                return GetAllowedVideoExtensions();
            else
                return GetAllowedDocumentExtensions();
        }
    }
}