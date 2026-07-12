using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.Product;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetPaged([FromQuery] ProductParameters parameters)
    {
        var result = await _productService.GetPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<ProductDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<ProductDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<ProductDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<ProductDto>.FailureResponse(result.Error!));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Products.Create)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse<ProductDto>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<ProductDto>.FailureResponse(result.Error!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Products.Update)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(int id, [FromBody] CreateProductDto dto)
    {
        var result = await _productService.UpdateAsync(id, dto);
        return result.Succeeded
            ? Ok(ApiResponse<ProductDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<ProductDto>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Products.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully."))
            : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}
