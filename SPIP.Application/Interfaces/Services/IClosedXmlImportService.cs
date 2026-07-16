using SPIP.Domain.Entities;
using SPIP.Shared.Result;
using System.IO;

namespace SPIP.Application.Interfaces.Services;

public interface IClosedXmlImportService
{
    Task<Result<PurchaseOrder>> ParsePurchaseOrderExcelAsync(Stream fileStream, int vendorId);
    Task<Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>> GetExcelPreviewAsync(Stream excelStream, int rowsToExtract = 50);
}
