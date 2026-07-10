namespace SPIP.Application.Interfaces.AI;

public interface IAIExtractionService
{
    Task<string> ExtractInvoiceDataAsync(Stream fileStream, string fileName);
}
