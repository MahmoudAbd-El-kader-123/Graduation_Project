using Hangfire;
using SPIP.Application.Interfaces.Services;

namespace SPIP.Infrastructure.BackgroundJobs;

public class InvoiceProcessingJob
{
    private readonly IInvoiceProcessingService _processingService;

    public InvoiceProcessingJob(IInvoiceProcessingService processingService)
    {
        _processingService = processingService;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [10, 30, 60])]
    public async Task ProcessAsync(int invoiceId, CancellationToken cancellationToken)
    {
        await _processingService.ProcessInvoiceAsync(invoiceId, cancellationToken);
    }
}
