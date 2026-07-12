using SPIP.Domain.Entities;
using SPIP.Shared.Result;
using System.IO;

namespace SPIP.Application.Interfaces.Services;

public interface IClosedXmlImportService
{
    Task<Result<PurchaseOrder>> ParsePurchaseOrderExcelAsync(Stream fileStream, int vendorId);
}
