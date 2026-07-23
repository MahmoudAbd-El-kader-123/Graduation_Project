namespace SPIP.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IInvoiceRepository Invoices { get; }
    IInvoiceProcessingLogRepository InvoiceProcessingLogs { get; }

    Task<int> SaveChangesAsync();
}
