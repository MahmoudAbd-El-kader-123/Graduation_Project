using AutoMapper;
using SPIP.Application.DTOs.Invoice;
using SPIP.Domain.Entities;

namespace SPIP.Application.Mapping;

public class InvoiceMappingProfile : Profile
{
    public InvoiceMappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UploadedAt, opt => opt.MapFrom(s => s.CreatedAt));

        CreateMap<Invoice, InvoiceDetailDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UploadedAt, opt => opt.MapFrom(s => s.CreatedAt));

        CreateMap<InvoiceItem, InvoiceItemDto>();

        CreateMap<Discrepancy, DiscrepancyDto>()
            .ForMember(d => d.DiscrepancyType, opt => opt.MapFrom(s => s.DiscrepancyType.ToString()));

        CreateMap<InvoiceProcessingLog, InvoiceProcessingLogDto>()
            .ForMember(d => d.FromStatus, opt => opt.MapFrom(s => s.FromStatus.HasValue ? s.FromStatus.Value.ToString() : null))
            .ForMember(d => d.ToStatus, opt => opt.MapFrom(s => s.ToStatus.ToString()))
            .ForMember(d => d.Timestamp, opt => opt.MapFrom(s => s.CreatedAt));

        CreateMap<Invoice, InvoiceListItemDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UploadedAt, opt => opt.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.UploadedByUserEmail, opt => opt.MapFrom(s => s.UploadedByUser != null ? s.UploadedByUser.Email : null));
    }
}
