namespace ElectionShield.Models
{
    public class AppSettings
    {
        public FileUploadSettings FileUpload { get; set; } = new();
        public AISettings AI { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
        public MapSettings Map { get; set; } = new();
    }

    public class FileUploadSettings
    {
        public long MaxFileSize { get; set; } = 10485760; // 10MB
        public string[] AllowedImageExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        public string[] AllowedVideoExtensions { get; set; } = { ".mp4", ".avi", ".mov", ".wmv", ".flv" };
        public string[] AllowedDocumentExtensions { get; set; } = { ".pdf", ".doc", ".docx", ".txt" };
        public string UploadPath { get; set; } = "uploads";
    }

    public class AISettings
    {
        public bool Enabled { get; set; } = false;
        public string Provider { get; set; } = "Azure";
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public decimal ConfidenceThreshold { get; set; } = 0.7m;
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = true;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "ElectionShield";
    }

    public class MapSettings
    {
        public string DefaultCenterLat { get; set; } = "20.5937";
        public string DefaultCenterLng { get; set; } = "78.9629";
        public int DefaultZoom { get; set; } = 5;
        public string TileLayer { get; set; } = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
        public string Attribution { get; set; } = "© OpenStreetMap contributors";
    }
}