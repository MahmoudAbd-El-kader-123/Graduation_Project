using SPIP.Application.DTOs.Product;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IProductService
{
    Task<Result<PagedResult<ProductDto>>> GetPagedAsync(ProductParameters parameters);
    Task<Result<ProductDto>> GetByIdAsync(int id);
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto);
    Task<Result<ProductDto>> UpdateAsync(int id, CreateProductDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}
