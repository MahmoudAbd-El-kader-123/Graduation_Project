using SPIP.Domain.Enums;

using SPIP.Shared.Pagination;

namespace SPIP.Application.DTOs.PurchaseOrder;

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public string? VendorName { get; set; }
    public int RequestedByUserId { get; set; }
    public string? RequestedByUserName { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class PurchaseOrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseOrderParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }
    public string? OrderNumber { get; set; }
    public int? VendorId { get; set; }
    public PurchaseOrderStatus? Status { get; set; }
}
