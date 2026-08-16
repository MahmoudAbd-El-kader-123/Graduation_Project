using Microsoft.AspNetCore.Http;

namespace SPIP.Application.DTOs.Invoice;

public class UploadInvoiceRequest
{
    public IFormFile File { get; set; } = null!;
    public int PurchaseOrderId { get; set; }
}
