using SPIP.Application.DTOs.Import;
using SPIP.Shared.Result;
using System.IO;

namespace SPIP.Application.Interfaces.Services;

public interface IClosedXmlImportService
{
    Task<Result<ParsedPurchaseOrderDto>> ParsePurchaseOrderExcelAsync(Stream fileStream, int vendorId, bool hasMixedVatRates = false);
    Task<Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>> GetExcelPreviewAsync(Stream excelStream, int rowsToExtract = 50);
}
