using FluentValidation;
using SPIP.Application.DTOs.Invoice;

namespace SPIP.Application.Validators;

public class UploadInvoiceRequestValidator : AbstractValidator<UploadInvoiceRequest>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public UploadInvoiceRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(f => f != null && f.Length > 0).WithMessage("File must not be empty.")
            .Must(f => f != null && f.Length <= MaxFileSizeBytes).WithMessage("File size must not exceed 10MB.")
            .Must(f => f != null && AllowedExtensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only PDF, JPG, JPEG, and PNG files are accepted.")
            .Must(f => f != null && !string.IsNullOrWhiteSpace(f.ContentType) && AllowedContentTypes.Contains(f.ContentType.ToLowerInvariant()))
            .WithMessage("File content type is not allowed.");

        RuleFor(x => x.PurchaseOrderId)
            .GreaterThan(0).WithMessage("A valid Purchase Order ID is required.");
    }
}
