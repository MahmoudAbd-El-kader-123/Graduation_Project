using ClosedXML.Excel;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Shared.Result;
using System.IO;

namespace SPIP.Infrastructure.Services;

public class ClosedXmlImportService : IClosedXmlImportService
{
    private readonly IVendorColumnMappingRepository _mappingRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public ClosedXmlImportService(
        IVendorColumnMappingRepository mappingRepository, 
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IUserRepository userRepository)
    {
        _mappingRepository = mappingRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task<Result<PurchaseOrder>> ParsePurchaseOrderExcelAsync(Stream fileStream, int vendorId)
    {
        if (fileStream == null || fileStream.Length == 0)
            return Result<PurchaseOrder>.Failure("File is empty.");

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null) return Result<PurchaseOrder>.Failure("No worksheet found.");

            // Get mapping for this vendor
            var mappings = await _mappingRepository.GetByVendorIdAsync(vendorId);
            if (!mappings.Any()) return Result<PurchaseOrder>.Failure("No column mappings found for this vendor.");

            // Find column indexes based on mappings by scanning the first 50 rows
            var columnIndexes = new Dictionary<string, int>();
            IXLRow? headerRow = null;

            for (int i = 1; i <= Math.Min(50, worksheet.LastRowUsed()?.RowNumber() ?? 50); i++)
            {
                var row = worksheet.Row(i);
                var cells = row.CellsUsed();
                
                bool isHeader = false;
                foreach (var mapping in mappings)
                {
                    if (cells.Any(c => c.GetString().Trim().Equals(mapping.ExcelColumn.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        isHeader = true;
                        break;
                    }
                }

                if (isHeader)
                {
                    headerRow = row;
                    break;
                }
            }

            if (headerRow == null) return Result<PurchaseOrder>.Failure("Could not locate the header row containing the mapped columns.");

            foreach (var mapping in mappings)
            {
                var cell = headerRow.CellsUsed().FirstOrDefault(c => c.GetString().Trim().Equals(mapping.ExcelColumn.Trim(), StringComparison.OrdinalIgnoreCase));
                if (cell != null)
                {
                    columnIndexes[mapping.SystemField] = cell.Address.ColumnNumber;
                }
            }

            // We need at minimum a Product Identifier (SKU, ERPID, Barcode) and Quantity.
            var hasProductIdentifier = columnIndexes.ContainsKey("ErpId") || columnIndexes.ContainsKey("Sku") || columnIndexes.ContainsKey("Barcode");
            if (!hasProductIdentifier || !columnIndexes.ContainsKey("Quantity"))
            {
                return Result<PurchaseOrder>.Failure("Required columns (Product Identifier and Quantity) are missing or not mapped correctly.");
            }

            int domainUserId = 1;
            if (_currentUserService.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdentityIdAsync(_currentUserService.UserId.Value);
                if (user != null) domainUserId = user.Id;
            }

            var po = new PurchaseOrder
            {
                OrderNumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss"), // Mock order number for now, ERP might provide it
                VendorId = vendorId,
                RequestedByUserId = domainUserId,
                Status = Domain.Enums.PurchaseOrderStatus.Draft,
                OrderDate = DateTime.UtcNow
            };

            var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
            decimal totalAmount = 0;

            // Load products for this vendor to match against
            // (In a real scenario with millions of products, you'd do a batch lookup based on the parsed codes)
            // For now, load all products of the vendor (assuming manageable size) or lookup per row.
            // Let's do a simple lookup list.
            var (vendorProducts, _) = await _productRepository.GetPagedAsync(new Application.DTOs.Product.ProductParameters { VendorId = vendorId, PageNumber = 1, PageSize = 10000 });

            var productsList = vendorProducts.ToList();

            for (int r = headerRow.RowNumber() + 1; r <= rowCount; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                string? productErpId = columnIndexes.ContainsKey("ErpId") ? row.Cell(columnIndexes["ErpId"]).GetString() : null;
                string? productSku = columnIndexes.ContainsKey("Sku") ? row.Cell(columnIndexes["Sku"]).GetString() : null;
                string? productBarcode = columnIndexes.ContainsKey("Barcode") ? row.Cell(columnIndexes["Barcode"]).GetString() : null;
                
                int quantity = 0;
                if (columnIndexes.ContainsKey("Quantity") && int.TryParse(row.Cell(columnIndexes["Quantity"]).GetString(), out var q))
                {
                    quantity = q;
                }

                decimal unitPrice = 0;
                if (columnIndexes.ContainsKey("UnitPrice") && decimal.TryParse(row.Cell(columnIndexes["UnitPrice"]).GetString(), out var u))
                {
                    unitPrice = u;
                }

                if (quantity <= 0) continue; // Skip invalid rows

                // Match Product
                var product = productsList.FirstOrDefault(p => 
                    (productErpId != null && p.ErpId == productErpId) ||
                    (productSku != null && p.Sku == productSku) ||
                    (productBarcode != null && p.Barcode == productBarcode)
                );

                if (product != null)
                {
                    // If unit price not mapped or zero, take from Product
                    if (unitPrice == 0) unitPrice = product.UnitPrice;

                    var lineTotal = unitPrice * quantity;
                    totalAmount += lineTotal;

                    po.Items.Add(new PurchaseOrderItem
                    {
                        ProductId = product.Id,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal
                    });
                }
            }

            po.TotalAmount = totalAmount;

            if (!po.Items.Any())
            {
                return Result<PurchaseOrder>.Failure("No valid items found in the Excel file that match the vendor's products.");
            }

            return Result<PurchaseOrder>.Success(po);
        }
        catch (Exception ex)
        {
            return Result<PurchaseOrder>.Failure($"Failed to process Excel file: {ex.Message}");
        }
    }
    public Task<Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>> GetExcelPreviewAsync(Stream excelStream, int rowsToExtract = 50)
    {
        if (excelStream == null || excelStream.Length == 0)
            return Task.FromResult(Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>.Failure("File is empty."));

        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null) 
                return Task.FromResult(Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>.Failure("No worksheet found."));

            var result = new SPIP.Application.DTOs.Import.ExcelPreviewDto();
            var maxRow = Math.Min(rowsToExtract, worksheet.LastRowUsed()?.RowNumber() ?? 0);
            
            result.TotalRowsFound = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (int r = 1; r <= maxRow; r++)
            {
                var row = worksheet.Row(r);
                var rowData = new List<string>();
                
                // Get the max column used in this row (or the whole sheet to keep grid square)
                var maxCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
                
                for (int c = 1; c <= maxCol; c++)
                {
                    rowData.Add(row.Cell(c).GetString().Trim());
                }
                
                result.DataGrid.Add(rowData);
            }

            return Task.FromResult(Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<SPIP.Application.DTOs.Import.ExcelPreviewDto>.Failure($"Failed to read Excel preview: {ex.Message}"));
        }
    }
}
