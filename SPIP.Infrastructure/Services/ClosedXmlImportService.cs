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

            // Find column indexes based on mappings
            var headerRow = worksheet.Row(1);
            var columnIndexes = new Dictionary<string, int>();

            foreach (var mapping in mappings)
            {
                var cell = headerRow.CellsUsed().FirstOrDefault(c => c.GetString().Equals(mapping.ExcelColumn, StringComparison.OrdinalIgnoreCase));
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

            var rowCount = worksheet.LastRowUsed().RowNumber();
            decimal totalAmount = 0;

            // Load products for this vendor to match against
            // (In a real scenario with millions of products, you'd do a batch lookup based on the parsed codes)
            // For now, load all products of the vendor (assuming manageable size) or lookup per row.
            // Let's do a simple lookup list.
            var (vendorProducts, _) = await _productRepository.GetPagedAsync(new Application.DTOs.Product.ProductParameters { VendorId = vendorId, PageNumber = 1, PageSize = 10000 });

            var productsList = vendorProducts.ToList();

            for (int r = 2; r <= rowCount; r++)
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
}
