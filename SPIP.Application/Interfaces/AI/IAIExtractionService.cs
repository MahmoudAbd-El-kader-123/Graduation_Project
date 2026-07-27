using SPIP.Application.DTOs.AI;

namespace SPIP.Application.Interfaces.AI;

public interface IAIExtractionService
{
    Task<AIExtractionResponseDto> ExtractInvoiceDataAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
