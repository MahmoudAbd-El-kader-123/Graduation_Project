using SPIP.Application.DTOs.PurchaseOrder;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;
using System.IO;

namespace SPIP.Application.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _poRepository;
    private readonly IClosedXmlImportService _importService;
    private readonly IVendorRepository _vendorRepository;

    public PurchaseOrderService(IPurchaseOrderRepository poRepository, IClosedXmlImportService importService, IVendorRepository vendorRepository)
    {
        _poRepository = poRepository;
        _importService = importService;
        _vendorRepository = vendorRepository;
    }

    public async Task<Result<PagedResult<PurchaseOrderDto>>> GetPagedAsync(PurchaseOrderParameters parameters)
    {
        var (items, totalCount) = await _poRepository.GetPagedAsync(parameters);
        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseOrderDto>>.Success(new PagedResult<PurchaseOrderDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        });
    }

    public async Task<Result<PurchaseOrderDto>> GetByIdAsync(int id)
    {
        var po = await _poRepository.GetWithItemsByIdAsync(id);
        if (po == null) return Result<PurchaseOrderDto>.Failure("Purchase Order not found.");

        return Result<PurchaseOrderDto>.Success(MapToDto(po));
    }

    public async Task<Result<int>> ImportFromExcelAsync(Stream fileStream, string fileName, int vendorId)
    {
        var vendor = await _vendorRepository.GetByIdAsync(vendorId);
        if (vendor == null) return Result<int>.Failure("Vendor not found.");

        var result = await _importService.ParsePurchaseOrderExcelAsync(fileStream, vendorId);
        if (!result.Succeeded) return Result<int>.Failure(result.Error!);

        var po = result.Data!;

        // Attempt to generate a unique order number if the parser created a mock one, or keep it.
        // If the order already exists by order number, you might reject or update. For simplicity, we just save.
        
        var created = await _poRepository.AddAsync(po);
        await _poRepository.SaveChangesAsync();

        return Result<int>.Success(created.Id);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var po = await _poRepository.GetByIdAsync(id);
        if (po == null) return Result<bool>.Failure("Purchase Order not found.");

        await _poRepository.DeleteAsync(po);
        await _poRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            VendorId = po.VendorId,
            VendorName = po.Vendor?.Name,
            RequestedByUserId = po.RequestedByUserId,
            RequestedByUserName = po.RequestedByUser?.FullName,
            Status = po.Status,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            TotalAmount = po.TotalAmount,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? i.Product?.ErpId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}
