using SPIP.Application.Interfaces.Repositories;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IUserRepository? _users;
    private IInvoiceRepository? _invoices;
    private IInvoiceProcessingLogRepository? _invoiceProcessingLogs;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IInvoiceRepository Invoices => _invoices ??= new InvoiceRepository(_context);
    public IInvoiceProcessingLogRepository InvoiceProcessingLogs => _invoiceProcessingLogs ??= new InvoiceProcessingLogRepository(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
