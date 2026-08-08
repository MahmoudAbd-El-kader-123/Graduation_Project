namespace SPIP.Infrastructure.Configuration;

public sealed class AIServiceSettings
{
    public const string SectionName = "AIService";

    /// <summary>Absolute base URL of the n8n instance. E.g. http://localhost:5678</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Relative path for the invoice extraction endpoint.</summary>
    public string ExtractionEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Full endpoint path for the AI chat webhook.
    /// Kept as ChatWebhookEndpoint to preserve the existing appsettings.json / environment variable binding.
    /// E.g. /webhook/ai-chat
    /// </summary>
    public string ChatWebhookEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional bearer secret for authenticating requests to the n8n webhook.
    /// Supply via environment variable or user-secrets — NOT appsettings.json in source control.
    /// Leave empty to skip Authorization header (existing behaviour if not yet configured).
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// HTTP timeout in seconds for AI service calls.
    /// Default 120 s — AI inference can take significantly longer than normal API calls.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
