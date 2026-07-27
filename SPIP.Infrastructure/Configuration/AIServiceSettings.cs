namespace SPIP.Infrastructure.Configuration;

public sealed class AIServiceSettings
{
    public const string SectionName = "AIService";

    public string BaseUrl { get; set; } = string.Empty;
    public string ExtractionEndpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}
