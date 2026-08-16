using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIP.Application.DTOs.AI;
using SPIP.Application.Interfaces.AI;
using SPIP.Infrastructure.Configuration;

namespace SPIP.Infrastructure.Services;

public class AIExtractionService : IAIExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _settings;
    private readonly ILogger<AIExtractionService> _logger;

    public AIExtractionService(
        HttpClient httpClient,
        IOptions<AIServiceSettings> settings,
        ILogger<AIExtractionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AIExtractionResponseDto> ExtractInvoiceDataAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
            content.Add(fileContent, "file", fileName);

            _logger.LogInformation("Sending invoice {FileName} to AI extraction service", fileName);

            _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
            }

            if (!string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.WebhookSecret);
            }

            using var response = await _httpClient.PostAsync(_settings.ExtractionEndpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"AI extraction failed with HTTP status {(int)response.StatusCode}.",
                    null,
                    response.StatusCode);

            AIExtractionResponseDto? extractionResponse;
            try
            {
                extractionResponse = JsonSerializer.Deserialize<AIExtractionResponseDto>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("AI extraction service returned invalid JSON.", ex);
            }

            if (extractionResponse is null)
                throw new HttpRequestException("AI extraction response could not be deserialized.");

            _logger.LogInformation("AI extraction completed for invoice {FileName}", fileName);
            return extractionResponse;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI extraction timed out for invoice {FileName}", fileName);
            throw new TimeoutException("AI extraction service timed out.", ex);
        }
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
}
