using UniSeapShop.Domain.DTOs.CategoryDTOs;
using UniSeapShop.Domain.DTOs.ProductDTOs;

namespace UniSeapShop.Application.Interfaces;

public interface IProductService
{
    Task<ProductDto> CreateProductAsunc(CreateProductDto createProductDto);
    Task<bool> DeleteProductAsync(Guid productId);
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<ProductDetailsDto> GetProductByIdAsync(Guid productId);
    Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductDto updateProductDto);
}