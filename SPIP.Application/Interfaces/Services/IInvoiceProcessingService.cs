namespace SPIP.Application.Interfaces.Services;

public interface IInvoiceProcessingService
{
    Task ProcessInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);
}
