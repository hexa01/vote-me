using Microsoft.AspNetCore.Mvc;
using ElectionShield.Services;
using System.Text.Json;
using ElectionShield.Services; 
namespace ElectionShield.Controllers
{
    [ApiController]
    [Route("webhook/whatsapp")]   // FIXED ROUTE
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly IReportService _reportService;
        private readonly ILogger<WhatsAppController> _logger;
        private readonly string _webhookVerifyToken;

        public WhatsAppController(
            IWhatsAppService whatsAppService,
            IReportService reportService,
            IConfiguration configuration,
            ILogger<WhatsAppController> logger)
        {
            _whatsAppService = whatsAppService;
            _reportService = reportService;  // FIXED injection
            _logger = logger;
            _webhookVerifyToken = configuration["WhatsApp:WebhookVerifyToken"] ?? string.Empty;
        }

        // WEBHOOK VERIFICATION (required by Meta)
        [HttpGet]
        public IActionResult VerifyWebhook(
            [FromQuery] string hub_mode,
            [FromQuery] string hub_challenge,
            [FromQuery] string hub_verify_token)
        {
            if (hub_mode == "subscribe" && hub_verify_token == _webhookVerifyToken)
            {
                return Ok(hub_challenge);
            }

            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveMessage()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation("\n📩 Incoming WhatsApp Message:\n{Body}", body);

                var data = JsonSerializer.Deserialize<WhatsAppWebhook>(body);

                var message = data?.Entry?.FirstOrDefault()?
                    .Changes?.FirstOrDefault()?
                    .Value?.Messages?.FirstOrDefault();

                if (message != null)
                {
                    var msg = new WhatsAppMessage
                    {
                        From = message.From,
                        Type = message.Type,
                        Text = message.Text,
                        Image = message.Image,
                        Video = message.Video,
                        Location = message.Location,
                        Interactive = message.Interactive
                    };

                    await _whatsAppService.ProcessIncomingMessageAsync(msg);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp webhook");
                return StatusCode(500, "Error processing message");
            }
        }

        // CREATE REPORT FROM WHATSAPP
        [HttpPost("report")]
        public async Task<IActionResult> CreateReportFromWhatsApp([FromBody] WhatsAppReportData reportData)
        {
            try
            {
                var report = await _whatsAppService.CreateReportFromWhatsAppAsync(reportData);

                return Ok(new
                {
                    success = true,
                    reportId = report.Id,
                    reportCode = report.ReportCode,
                    message = "Report created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report from WhatsApp data");
                return BadRequest(new { success = false, error = "Failed to create report" });
            }
        }

        // GET REPORT STATUS
        [HttpGet("report/{reportCode}")]
        public async Task<IActionResult> GetReportStatus(string reportCode)
        {
            try
            {
                var report = await _reportService.GetReportByCodeAsync(reportCode);

                if (report == null)
                {
                    return NotFound(new { success = false, error = "Report not found" });
                }

                return Ok(new
                {
                    success = true,
                    reportCode = report.ReportCode,
                    status = report.Status.ToString(),
                    title = report.Title,
                    createdAt = report.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report status");
                return BadRequest(new { success = false, error = "Failed to get report status" });
            }
        }
    }
}
