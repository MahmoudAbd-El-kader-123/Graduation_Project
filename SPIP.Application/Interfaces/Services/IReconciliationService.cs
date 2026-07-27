namespace SPIP.Application.Interfaces.Services;

public interface IReconciliationService
{
    Task ReconcileAsync(int invoiceId, CancellationToken cancellationToken = default);
}
