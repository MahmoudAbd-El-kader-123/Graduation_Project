using ClosedXML.Excel;
using SPIP.Application.DTOs.Import;
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

    public async Task<Result<ParsedPurchaseOrderDto>> ParsePurchaseOrderExcelAsync(Stream fileStream, int vendorId, bool hasMixedVatRates = false)
    {
        if (fileStream == null || fileStream.Length == 0)
            return Result<ParsedPurchaseOrderDto>.Failure("File is empty.");

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null) return Result<ParsedPurchaseOrderDto>.Failure("No worksheet found.");

            // Get mapping for this vendor
            var mappings = await _mappingRepository.GetByVendorIdAsync(vendorId);
            if (!mappings.Any()) return Result<ParsedPurchaseOrderDto>.Failure("No column mappings found for this vendor.");

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

            if (headerRow == null) return Result<ParsedPurchaseOrderDto>.Failure("Could not locate the header row containing the mapped columns.");

            foreach (var mapping in mappings)
            {
                var cell = headerRow.CellsUsed().FirstOrDefault(c => c.GetString().Trim().Equals(mapping.ExcelColumn.Trim(), StringComparison.OrdinalIgnoreCase));
                if (cell != null)
                {
                    columnIndexes[mapping.SystemField] = cell.Address.ColumnNumber;
                }
            }

            // We need at minimum SkuSupplier, Quantity, and UnitPrice.
            if (!columnIndexes.ContainsKey("SkuSupplier") || !columnIndexes.ContainsKey("Quantity") || !columnIndexes.ContainsKey("UnitPrice"))
            {
                return Result<ParsedPurchaseOrderDto>.Failure("Required columns (SKU Supplier, Quantity, Price) are missing or not mapped correctly.");
            }

            int domainUserId = 1;
            if (_currentUserService.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdentityIdAsync(_currentUserService.UserId.Value);
                if (user != null) domainUserId = user.Id;
            }

            var po = new PurchaseOrder
            {
                OrderNumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                VendorId = vendorId,
                RequestedByUserId = domainUserId,
                Status = Domain.Enums.PurchaseOrderStatus.Draft,
                OrderDate = DateTime.UtcNow
            };

            var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
            decimal totalAmount = 0;

            // Load products for this vendor
            var (vendorProducts, _) = await _productRepository.GetPagedAsync(new Application.DTOs.Product.ProductParameters { VendorId = vendorId, PageNumber = 1, PageSize = 10000 });
            var productsList = vendorProducts.ToList();
            var productsCreated = 0;
            var productsMatched = 0;
            var rowsSkipped = 0;

            // --- SMART VAT DEDUCTION ALGORITHM ---
            decimal? deducedVatPercentage = null;
            bool useDeduction = false;

            if (!hasMixedVatRates && columnIndexes.ContainsKey("VatAmount"))
            {
                var first3Vats = new List<decimal>();
                for (int r = headerRow.RowNumber() + 1; r <= Math.Min(headerRow.RowNumber() + 3, rowCount); r++)
                {
                    var row = worksheet.Row(r);
                    if (row.IsEmpty()) continue;

                    if (int.TryParse(row.Cell(columnIndexes["Quantity"]).GetString(), out var q) &&
                        decimal.TryParse(row.Cell(columnIndexes["UnitPrice"]).GetString(), out var p) &&
                        decimal.TryParse(row.Cell(columnIndexes["VatAmount"]).GetString(), out var v))
                    {
                        var lineTotal = q * p;
                        if (lineTotal > 0)
                        {
                            var vatPerc = Math.Round((v / lineTotal) * 100m, 2);
                            first3Vats.Add(vatPerc);
                        }
                    }
                }

                // If we found VAT in the first rows and they all match identically, use it!
                if (first3Vats.Count > 0 && first3Vats.All(v => v == first3Vats.First()))
                {
                    deducedVatPercentage = first3Vats.First();
                    useDeduction = true;
                }
            }
            // -------------------------------------

            for (int r = headerRow.RowNumber() + 1; r <= rowCount; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                var importedRow = ReadProductRow(row, columnIndexes);
                if (importedRow is null)
                {
                    rowsSkipped++;
                    continue;
                }

                var productResolution = await ResolveProductAsync(productsList, importedRow, vendorId);
                productsCreated += productResolution.WasCreated ? 1 : 0;
                productsMatched += productResolution.WasCreated ? 0 : 1;

                var lineTotal = importedRow.UnitPrice * importedRow.Quantity;
                decimal vatAmount = 0;
                decimal vatPercentage = 0;

                if (useDeduction && deducedVatPercentage.HasValue)
                {
                    vatPercentage = deducedVatPercentage.Value;
                    vatAmount = lineTotal * (vatPercentage / 100m);
                }
                else if (columnIndexes.ContainsKey("VatAmount") && decimal.TryParse(row.Cell(columnIndexes["VatAmount"]).GetString(), out var v))
                {
                    vatAmount = v;
                    vatPercentage = lineTotal > 0 ? (vatAmount / lineTotal) * 100m : 0;
                }

                var finalAmount = lineTotal + vatAmount;
                totalAmount += finalAmount;

                po.Items.Add(new PurchaseOrderItem
                {
                    Product = productResolution.Product,
                    Quantity = importedRow.Quantity,
                    UnitPrice = importedRow.UnitPrice,
                    LineTotal = lineTotal,
                    VatPercentage = Math.Round(vatPercentage, 2),
                    VatAmount = Math.Round(vatAmount, 2),
                    Amount = Math.Round(finalAmount, 2)
                });
            }

            po.TotalAmount = Math.Round(totalAmount, 2);

            if (!po.Items.Any())
            {
                return Result<ParsedPurchaseOrderDto>.Failure("No valid items found in the Excel file.");
            }

            return Result<ParsedPurchaseOrderDto>.Success(new ParsedPurchaseOrderDto(
                po,
                productsCreated,
                productsMatched,
                rowsSkipped));
        }
        catch (Exception ex)
        {
            return Result<ParsedPurchaseOrderDto>.Failure($"Failed to process Excel file: {ex.Message}");
        }
    }

    private static string? GetOptionalCell(IXLRow row, IReadOnlyDictionary<string, int> columnIndexes, string systemField)
    {
        return columnIndexes.TryGetValue(systemField, out var columnIndex)
            ? row.Cell(columnIndex).GetString().Trim()
            : null;
    }

    private static ImportedProductRow? ReadProductRow(IXLRow row, IReadOnlyDictionary<string, int> columnIndexes)
    {
        var supplierSku = GetOptionalCell(row, columnIndexes, "SkuSupplier");
        var quantityText = GetOptionalCell(row, columnIndexes, "Quantity");
        var unitPriceText = GetOptionalCell(row, columnIndexes, "UnitPrice");

        if (string.IsNullOrWhiteSpace(supplierSku) ||
            !int.TryParse(quantityText, out var quantity) ||
            !decimal.TryParse(unitPriceText, out var unitPrice) ||
            quantity <= 0 ||
            unitPrice <= 0)
            return null;

        return new ImportedProductRow
        {
            SupplierSku = supplierSku,
            Barcode = GetOptionalCell(row, columnIndexes, "Barcode"),
            Description = GetOptionalCell(row, columnIndexes, "Description"),
            Uom = GetOptionalCell(row, columnIndexes, "Uom"),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    private async Task<ProductResolution> ResolveProductAsync(
        List<Product> vendorProducts,
        ImportedProductRow importedRow,
        int vendorId)
    {
        var product = vendorProducts.FirstOrDefault(candidate =>
            string.Equals(candidate.SkuSupplier, importedRow.SupplierSku, StringComparison.OrdinalIgnoreCase));

        product ??= vendorProducts.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(importedRow.Barcode) &&
            string.Equals(candidate.Barcode, importedRow.Barcode, StringComparison.OrdinalIgnoreCase));

        if (product is not null)
            return new ProductResolution(product, false);

        product = CreateProduct(importedRow, vendorId);
        await _productRepository.AddAsync(product);
        vendorProducts.Add(product);
        return new ProductResolution(product, true);
    }

    private static Product CreateProduct(ImportedProductRow importedRow, int vendorId) =>
        new()
        {
            VendorId = vendorId,
            ErpId = importedRow.SupplierSku,
            SkuSupplier = importedRow.SupplierSku,
            Barcode = importedRow.Barcode,
            Name = string.IsNullOrWhiteSpace(importedRow.Description) ? importedRow.SupplierSku : importedRow.Description,
            Description = importedRow.Description,
            Uom = string.IsNullOrWhiteSpace(importedRow.Uom) ? "PCS" : importedRow.Uom,
            UnitPrice = importedRow.UnitPrice
        };

    private sealed class ImportedProductRow
    {
        public required string SupplierSku { get; init; }
        public string? Barcode { get; init; }
        public string? Description { get; init; }
        public string? Uom { get; init; }
        public required int Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
    }

    private sealed record ProductResolution(Product Product, bool WasCreated);
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
