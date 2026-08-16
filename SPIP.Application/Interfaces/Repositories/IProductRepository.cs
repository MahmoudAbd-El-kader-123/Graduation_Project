using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Application.DTOs.Product;

namespace SPIP.Application.Interfaces.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductParameters parameters);
}
