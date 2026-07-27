namespace SPIP.Application.DTOs.PurchaseOrder;

public sealed class PurchaseOrderImportResultDto
{
    public int PurchaseOrderId { get; init; }
    public int ItemsImported { get; init; }
    public int ProductsCreated { get; init; }
    public int ProductsMatched { get; init; }
    public int RowsSkipped { get; init; }
}
