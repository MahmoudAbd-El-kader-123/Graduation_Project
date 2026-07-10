namespace SPIP.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }

    Task<int> SaveChangesAsync();
}
