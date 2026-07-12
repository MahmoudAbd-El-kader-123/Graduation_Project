using SPIP.Application.DTOs.Product;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IVendorRepository _vendorRepository;

    public ProductService(IProductRepository productRepository, IVendorRepository vendorRepository)
    {
        _productRepository = productRepository;
        _vendorRepository = vendorRepository;
    }

    public async Task<Result<PagedResult<ProductDto>>> GetPagedAsync(ProductParameters parameters)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(parameters);
        return Result<PagedResult<ProductDto>>.Success(new PagedResult<ProductDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        });
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return Result<ProductDto>.Failure("Product not found.");
        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto)
    {
        var vendor = await _vendorRepository.GetByIdAsync(dto.VendorId);
        if (vendor == null) return Result<ProductDto>.Failure("Vendor not found.");

        var product = new Product
        {
            ErpId = dto.ErpId,
            Name = dto.Name,
            Sku = dto.Sku,
            Barcode = dto.Barcode,
            Description = dto.Description,
            UnitPrice = dto.UnitPrice,
            Uom = dto.Uom,
            VendorId = dto.VendorId
        };

        var created = await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();
        
        // Load vendor for DTO mapping
        created.Vendor = vendor;
        return Result<ProductDto>.Success(MapToDto(created));
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, CreateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return Result<ProductDto>.Failure("Product not found.");
        
        if (product.VendorId != dto.VendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(dto.VendorId);
            if (vendor == null) return Result<ProductDto>.Failure("Vendor not found.");
        }

        product.ErpId = dto.ErpId;
        product.Name = dto.Name;
        product.Sku = dto.Sku;
        product.Barcode = dto.Barcode;
        product.Description = dto.Description;
        product.UnitPrice = dto.UnitPrice;
        product.Uom = dto.Uom;
        product.VendorId = dto.VendorId;

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();
        
        // reload for DTO
        product = await _productRepository.GetByIdAsync(id);
        return Result<ProductDto>.Success(MapToDto(product!));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return Result<bool>.Failure("Product not found.");

        await _productRepository.DeleteAsync(product);
        await _productRepository.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        ErpId = p.ErpId,
        Name = p.Name,
        Sku = p.Sku,
        Barcode = p.Barcode,
        Description = p.Description,
        UnitPrice = p.UnitPrice,
        Uom = p.Uom,
        VendorId = p.VendorId,
        VendorName = p.Vendor?.Name
    };
}
