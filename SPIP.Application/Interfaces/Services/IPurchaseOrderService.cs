using SPIP.Application.DTOs.PurchaseOrder;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;
using System.IO;

namespace SPIP.Application.Interfaces.Services;

public interface IPurchaseOrderService
{
    Task<Result<PagedResult<PurchaseOrderDto>>> GetPagedAsync(PurchaseOrderParameters parameters);
    Task<Result<PurchaseOrderDto>> GetByIdAsync(int id);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<int>> ImportFromExcelAsync(Stream fileStream, string fileName, int vendorId);
}
