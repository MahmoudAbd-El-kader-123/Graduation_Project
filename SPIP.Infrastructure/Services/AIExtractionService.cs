using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPIP.Application.DTOs.AI;
using SPIP.Application.Interfaces.AI;

namespace SPIP.Infrastructure.Services;

public class AIExtractionService : IAIExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIExtractionService> _logger;

    public AIExtractionService(HttpClient httpClient, ILogger<AIExtractionService> logger)
    {
        _httpClient = httpClient;
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
            using var response = await _httpClient.PostAsync("/extract", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"AI extraction failed with status {(int)response.StatusCode}: {responseBody}");

            var result = JsonSerializer.Deserialize<AIExtractionResponseDto>(responseBody, JsonOptions);
            if (result is null)
                throw new HttpRequestException("AI extraction response could not be deserialized.");

            _logger.LogInformation("AI extraction completed for invoice {FileName}", fileName);
            return result;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AI extraction timed out for invoice {FileName}", fileName);
            throw;
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
