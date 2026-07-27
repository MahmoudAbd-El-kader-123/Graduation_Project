using SPIP.Application.DTOs.Vendor;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Services;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;

    public VendorService(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    public async Task<Result<PagedResult<VendorDto>>> GetPagedAsync(VendorParameters parameters)
    {
        var (items, totalCount) = await _vendorRepository.GetPagedAsync(parameters);
        return Result<PagedResult<VendorDto>>.Success(new PagedResult<VendorDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        });
    }

    public async Task<Result<VendorDto>> GetByIdAsync(int id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null) return Result<VendorDto>.Failure("Vendor not found.");
        return Result<VendorDto>.Success(MapToDto(vendor));
    }

    public async Task<Result<VendorDto>> CreateAsync(CreateVendorDto dto)
    {
        var vendor = new Vendor
        {
            ErpId = dto.ErpId,
            Name = dto.Name,
            TaxRegistrationNumber = dto.TaxRegistrationNumber,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            Address = dto.Address,
            IsApproved = true
        };

        var created = await _vendorRepository.AddAsync(vendor);
        await _vendorRepository.SaveChangesAsync();
        return Result<VendorDto>.Success(MapToDto(created));
    }

    public async Task<Result<VendorDto>> UpdateAsync(int id, CreateVendorDto dto)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null) return Result<VendorDto>.Failure("Vendor not found.");

        vendor.ErpId = dto.ErpId;
        vendor.Name = dto.Name;
        vendor.TaxRegistrationNumber = dto.TaxRegistrationNumber;
        vendor.ContactEmail = dto.ContactEmail;
        vendor.ContactPhone = dto.ContactPhone;
        vendor.Address = dto.Address;

        await _vendorRepository.UpdateAsync(vendor);
        await _vendorRepository.SaveChangesAsync();
        return Result<VendorDto>.Success(MapToDto(vendor));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null) return Result<bool>.Failure("Vendor not found.");

        await _vendorRepository.DeleteAsync(vendor);
        await _vendorRepository.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ToggleApprovalStatusAsync(int id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null) return Result<bool>.Failure("Vendor not found.");

        vendor.IsApproved = !vendor.IsApproved;
        await _vendorRepository.UpdateAsync(vendor);
        await _vendorRepository.SaveChangesAsync();
        return Result<bool>.Success(vendor.IsApproved);
    }

    private static VendorDto MapToDto(Vendor v) => new()
    {
        Id = v.Id,
        ErpId = v.ErpId,
        Name = v.Name,
        TaxRegistrationNumber = v.TaxRegistrationNumber,
        ContactEmail = v.ContactEmail,
        ContactPhone = v.ContactPhone,
        Address = v.Address,
        IsApproved = v.IsApproved
    };
}
