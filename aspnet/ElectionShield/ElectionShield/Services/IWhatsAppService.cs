using ElectionShield.Models;
using ElectionShield.ViewModels;
using System.Text.Json;

namespace ElectionShield.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendWelcomeMessageAsync(string phoneNumber);
        Task<bool> ProcessIncomingMessageAsync(WhatsAppMessage message);
        Task<Report> CreateReportFromWhatsAppAsync(WhatsAppReportData data);
        Task<bool> SendReportConfirmationAsync(string phoneNumber, string reportCode);
        Task<bool> RequestLocationAsync(string phoneNumber);
        Task<bool> RequestMediaAsync(string phoneNumber);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IReportService _reportService;
        private readonly IFileService _fileService;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(HttpClient httpClient, IConfiguration configuration,
            IReportService reportService, IFileService fileService, ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _reportService = reportService;
            _fileService = fileService;
            _logger = logger;

            // Configure HttpClient for WhatsApp API
            _httpClient.BaseAddress = new Uri("");
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                $"Bearer {_configuration["WhatsApp:AccessToken"]}");
        }

        public async Task<bool> SendWelcomeMessageAsync(string phoneNumber)
        {
            try
            {
                var message = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "interactive",
                    interactive = new
                    {
                        type = "button",
                        body = new { text = "Welcome to ElectionShield! 🛡️\n\nReport election incidents securely and anonymously. How can we help you today?" },
                        action = new
                        {
                            buttons = new[]
                            {
                                new
                                {
                                    type = "reply",
                                    reply = new { id = "report_incident", title = "📝 Report Incident" }
                                },
                                new
                                {
                                    type = "reply",
                                    reply = new { id = "track_report", title = "🔍 Track Report" }
                                },
                                new
                                {
                                    type = "reply",
                                    reply = new { id = "get_help", title = "ℹ️ Get Help" }
                                }
                            }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_configuration["WhatsApp:PhoneNumberId"]}/messages", message);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome message to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> ProcessIncomingMessageAsync(WhatsAppMessage message)
        {
            try
            {
                switch (message.Type)
                {
                    case "text":
                        return await ProcessTextMessageAsync(message);
                    case "image":
                    case "video":
                    case "document":
                        return await ProcessMediaMessageAsync(message);
                    case "location":
                        return await ProcessLocationMessageAsync(message);
                    case "interactive":
                        return await ProcessInteractiveMessageAsync(message);
                    default:
                        _logger.LogWarning("Unsupported message type: {MessageType}", message.Type);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp message");
                return false;
            }
        }

        private async Task<bool> ProcessTextMessageAsync(WhatsAppMessage message)
        {
            var text = message.Text?.Body?.ToLower() ?? "";

            if (text.Contains("report") || text.Contains("incident"))
            {
                return await StartReportFlowAsync(message.From);
            }
            else if (text.Contains("track") || text.Contains("status"))
            {
                return await RequestReportCodeAsync(message.From);
            }
            else if (text.Contains("help"))
            {
                return await SendHelpMessageAsync(message.From);
            }
            else
            {
                return await SendWelcomeMessageAsync(message.From);
            }
        }

        private async Task<bool> StartReportFlowAsync(string phoneNumber)
        {
            var message = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    header = new { type = "text", text = "Select Incident Type" },
                    body = new { text = "Please choose the category of incident you want to report:" },
                    action = new
                    {
                        button = "Choose Category",
                        sections = new[]
                        {
                            new
                            {
                                title = "Election Violations",
                                rows = new[]
                                {
                                    new { id = "voter_intimidation", title = "🚫 Voter Intimidation", description = "Threats or coercion of voters" },
                                    new { id = "ballot_tampering", title = "🗳️ Ballot Tampering", description = "Unauthorized handling of ballots" },
                                    new { id = "polling_issues", title = "🏛️ Polling Station Issues", description = "Problems at voting locations" }
                                }
                            },
                            new
                            {
                                title = "Other Issues",
                                rows = new[]
                                {
                                    new { id = "campaign_violation", title = "📢 Campaign Violation", description = "Illegal campaigning activities" },
                                    new { id = "technical_issue", title = "💻 Technical Issues", description = "EVMs or voting system problems" },
                                    new { id = "other", title = "❓ Other", description = "Other election-related issues" }
                                }
                            }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_configuration["WhatsApp:PhoneNumberId"]}/messages", message);

            return response.IsSuccessStatusCode;
        }

        public async Task<Report> CreateReportFromWhatsAppAsync(WhatsAppReportData data)
        {
            try
            {
                var reportModel = new CreateReportViewModel
                {
                    Title = data.IncidentTitle,
                    Description = data.Description,
                    Category = data.Category,
                    Location = data.Location,
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    IsAnonymous = true, // WhatsApp reports are anonymous by default
                    ReporterPhone = data.PhoneNumber
                };

                var report = await _reportService.CreateReportAsync(reportModel);

                // Save media files if any
                if (data.MediaUrls != null && data.MediaUrls.Any())
                {
                    foreach (var mediaUrl in data.MediaUrls)
                    {
                        // Download and save media file
                        await ProcessAndSaveMediaAsync(mediaUrl, report.Id);
                    }
                }

                // Send confirmation message
                await SendReportConfirmationAsync(data.PhoneNumber, report.ReportCode);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report from WhatsApp");
                throw;
            }
        }

        public async Task<bool> SendReportConfirmationAsync(string phoneNumber, string reportCode)
        {
            var message = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "template",
                template = new
                {
                    name = "report_submission_confirmation",
                    language = new { code = "en" },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new[]
                            {
                                new { type = "text", text = reportCode }
                            }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_configuration["WhatsApp:PhoneNumberId"]}/messages", message);

            if (response.IsSuccessStatusCode)
            {
                // Also send a follow-up text message
                var followUpMessage = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = $"Your report has been submitted successfully! 🛡️\n\n📋 Report Code: {reportCode}\n\nYou can track your report status using this code on our website or by replying 'TRACK {reportCode}' to this number.\n\nThank you for helping protect election integrity! 🇮🇳" }
                };

                await _httpClient.PostAsJsonAsync(
                    $"{_configuration["WhatsApp:PhoneNumberId"]}/messages", followUpMessage);
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RequestLocationAsync(string phoneNumber)
        {
            var message = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "text",
                text = new { body = "📍 Please share your location to help us pinpoint the incident.\n\nClick the location icon next to the text input and share your current location." }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_configuration["WhatsApp:PhoneNumberId"]}/messages", message);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RequestMediaAsync(string phoneNumber)
        {
            var message = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "text",
                text = new { body = "📎 You can now share photos or videos as evidence.\n\nPlease send any images or videos related to the incident. You can send multiple files." }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_configuration["WhatsApp:PhoneNumberId"]}/messages", message);

            return response.IsSuccessStatusCode;
        }

        private async Task ProcessAndSaveMediaAsync(string mediaUrl, Guid reportId)
        {
            try
            {
                // Download media from WhatsApp
                var mediaResponse = await _httpClient.GetAsync(mediaUrl);
                if (mediaResponse.IsSuccessStatusCode)
                {
                    var mediaData = await mediaResponse.Content.ReadAsByteArrayAsync();
                    var fileName = $"whatsapp_evidence_{Guid.NewGuid()}.jpg";

                    // Save to temporary file
                    var tempPath = Path.GetTempFileName();
                    await File.WriteAllBytesAsync(tempPath, mediaData);

                    // Create form file and save using existing service
                    var formFile = new FormFile(
                        new FileStream(tempPath, FileMode.Open),
                        0,
                        mediaData.Length,
                        "whatsapp_media",
                        fileName);

                    var savedPath = await _fileService.SaveFileAsync(formFile, "whatsapp_reports");

                    // Create media file record
                    // This would be saved to your database
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp media");
            }
        }

        // Helper methods for other message types...
        private Task<bool> ProcessMediaMessageAsync(WhatsAppMessage message) => Task.FromResult(true);
        private Task<bool> ProcessLocationMessageAsync(WhatsAppMessage message) => Task.FromResult(true);
        private Task<bool> ProcessInteractiveMessageAsync(WhatsAppMessage message) => Task.FromResult(true);
        private Task<bool> RequestReportCodeAsync(string phoneNumber) => Task.FromResult(true);
        private Task<bool> SendHelpMessageAsync(string phoneNumber) => Task.FromResult(true);
    }

    public class WhatsAppMessage
    {
        public string From { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public WhatsAppText? Text { get; set; }
        public WhatsAppMedia? Image { get; set; }
        public WhatsAppMedia? Video { get; set; }
        public WhatsAppLocation? Location { get; set; }
        public WhatsAppInteractive? Interactive { get; set; }
    }

    public class WhatsAppText
    {
        public string Body { get; set; } = string.Empty;
    }

    public class WhatsAppMedia
    {
        public string Id { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
    }

    public class WhatsAppLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class WhatsAppInteractive
    {
        public string Type { get; set; } = string.Empty;
        public WhatsAppButtonReply? ButtonReply { get; set; }
        public WhatsAppListReply? ListReply { get; set; }
    }

    public class WhatsAppButtonReply
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class WhatsAppListReply
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class WhatsAppReportData
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string IncidentTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> MediaUrls { get; set; } = new();
    }
}