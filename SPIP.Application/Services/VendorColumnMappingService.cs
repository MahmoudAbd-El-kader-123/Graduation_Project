using SPIP.Application.DTOs.VendorColumnMapping;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Shared.Result;

namespace SPIP.Application.Services;

public class VendorColumnMappingService : IVendorColumnMappingService
{
    private readonly IVendorColumnMappingRepository _mappingRepository;
    private readonly IVendorRepository _vendorRepository;

    public VendorColumnMappingService(IVendorColumnMappingRepository mappingRepository, IVendorRepository vendorRepository)
    {
        _mappingRepository = mappingRepository;
        _vendorRepository = vendorRepository;
    }

    public async Task<Result<IReadOnlyList<VendorColumnMappingDto>>> GetByVendorIdAsync(int vendorId)
    {
        var mappings = await _mappingRepository.GetByVendorIdAsync(vendorId);
        var dtos = mappings.Select(m => new VendorColumnMappingDto
        {
            Id = m.Id,
            VendorId = m.VendorId,
            SystemField = m.SystemField,
            ExcelColumn = m.ExcelColumn
        }).ToList();

        return Result<IReadOnlyList<VendorColumnMappingDto>>.Success(dtos);
    }

    public async Task<Result<VendorColumnMappingDto>> CreateOrUpdateAsync(CreateVendorColumnMappingDto dto)
    {
        var vendor = await _vendorRepository.GetByIdAsync(dto.VendorId);
        if (vendor == null) return Result<VendorColumnMappingDto>.Failure("Vendor not found.");

        var existingMapping = await _mappingRepository.GetBySystemFieldAsync(dto.VendorId, dto.SystemField);

        if (existingMapping != null)
        {
            // Update existing
            existingMapping.ExcelColumn = dto.ExcelColumn;
            await _mappingRepository.UpdateAsync(existingMapping);
            await _mappingRepository.SaveChangesAsync();

            return Result<VendorColumnMappingDto>.Success(new VendorColumnMappingDto
            {
                Id = existingMapping.Id,
                VendorId = existingMapping.VendorId,
                SystemField = existingMapping.SystemField,
                ExcelColumn = existingMapping.ExcelColumn
            });
        }
        else
        {
            // Create new
            var newMapping = new VendorColumnMapping
            {
                VendorId = dto.VendorId,
                SystemField = dto.SystemField,
                ExcelColumn = dto.ExcelColumn
            };

            var created = await _mappingRepository.AddAsync(newMapping);
            await _mappingRepository.SaveChangesAsync();

            return Result<VendorColumnMappingDto>.Success(new VendorColumnMappingDto
            {
                Id = created.Id,
                VendorId = created.VendorId,
                SystemField = created.SystemField,
                ExcelColumn = created.ExcelColumn
            });
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var mapping = await _mappingRepository.GetByIdAsync(id);
        if (mapping == null) return Result<bool>.Failure("Mapping not found.");

        await _mappingRepository.DeleteAsync(mapping);
        await _mappingRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
