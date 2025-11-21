using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectionShield.Services
{
    public static class AiResponseParser
    {
        public static AiAnalysisData? ParseAnalysis(string? aiJson)
        {
            if (string.IsNullOrEmpty(aiJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<AiAnalysisData>(aiJson);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static double GetRiskScore(string? aiJson)
        {
            var analysis = ParseAnalysis(aiJson);
            return analysis?.RiskScore ?? 0.0;
        }

        public static string[] GetTags(string? aiJson)
        {
            var analysis = ParseAnalysis(aiJson);
            return analysis?.AiTags ?? Array.Empty<string>();
        }

        public static string GetSummary(string? aiJson)
        {
            var analysis = ParseAnalysis(aiJson);
            return analysis?.Summary ?? "No AI analysis available";
        }

        public static AiRiskLevel GetRiskLevel(string? aiJson)
        {
            var riskScore = GetRiskScore(aiJson);
            return riskScore switch
            {
                >= 0.9 => AiRiskLevel.Critical,
                >= 0.7 => AiRiskLevel.High,
                >= 0.3 => AiRiskLevel.Medium,
                _ => AiRiskLevel.Low
            };
        }
    }

    public class AiAnalysisData
    {
        [JsonPropertyName("ai_tags")]
        public string[] AiTags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("risk_score")]
        public double RiskScore { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        // Additional fields that might come from AI
        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [JsonPropertyName("entities")]
        public string[]? Entities { get; set; }

        [JsonPropertyName("sentiment")]
        public string? Sentiment { get; set; }

        [JsonPropertyName("moderation_flags")]
        public string[]? ModerationFlags { get; set; }
    }

    public enum AiRiskLevel
    {
        Low = 0,      // 0.0 - 0.3
        Medium = 1,   // 0.3 - 0.7
        High = 2,     // 0.7 - 0.9
        Critical = 3  // 0.9 - 1.0
    }
}