namespace SPIP.Application.DTOs.Import;

public sealed record ParsedPurchaseOrderDto(
    SPIP.Domain.Entities.PurchaseOrder PurchaseOrder,
    int ProductsCreated,
    int ProductsMatched,
    int RowsSkipped);
