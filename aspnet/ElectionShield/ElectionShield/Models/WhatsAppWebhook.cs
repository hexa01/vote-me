namespace ElectionShield.Services
{
    public class WhatsAppWebhook
    {
        public List<WebhookEntry> Entry { get; set; } = new();
    }

    public class WebhookEntry
    {
        public string Id { get; set; } = string.Empty;
        public List<WebhookChange> Changes { get; set; } = new();
    }

    public class WebhookChange
    {
        public WebhookValue Value { get; set; } = new();
        public string Field { get; set; } = string.Empty;
    }

    public class WebhookValue
    {
        public string MessagingProduct { get; set; } = string.Empty;
        public List<WebhookMessage> Messages { get; set; } = new();
    }

    public class WebhookMessage
    {
        public string From { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public WhatsAppText? Text { get; set; }
        public WhatsAppMedia? Image { get; set; }
        public WhatsAppMedia? Video { get; set; }
        public WhatsAppLocation? Location { get; set; }
        public WhatsAppInteractive? Interactive { get; set; }
    }
}
