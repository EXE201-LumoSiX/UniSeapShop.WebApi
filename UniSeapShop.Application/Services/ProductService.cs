using Microsoft.EntityFrameworkCore;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.CategoryDTOs;
using UniSeapShop.Domain.DTOs.ProductDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlobService _blobService;

        public ProductService(IUnitOfWork unitOfWork, ILoggerService loggerService, IBlobService blobService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _blobService = blobService;
        }

        public async Task<ProductDto> CreateProductAsunc(CreateProductDto createProductDto)
        {
            var fileName = $"blindbox-thumbnails/thumbnails-{Guid.NewGuid()}{Path.GetExtension(createProductDto.ImageFile.FileName)}";
            await using var stream = createProductDto.ImageFile.OpenReadStream();
            await _blobService.UploadFileAsync(fileName, stream);

            var imageUrl = await _blobService.GetPreviewUrlAsync(fileName);
            if (string.IsNullOrEmpty(imageUrl))
                throw ErrorHelper.Internal("Not found imageUrl");
            var product = new Product
            {
                ProductName = createProductDto.ProductName,
                Description = createProductDto.Description,
                ProductImage = imageUrl,
                UsageHistory = createProductDto.UsageHistory,
                Price = createProductDto.Price,
                CategoryId = createProductDto.CategoryId,
                Quantity = createProductDto.Quantity,
                Supplier = GetSupplierbyIdAsync(createProductDto.SupplierId).Result,
                Category = GetCategoryByIdAsync(createProductDto.CategoryId).Result
            };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success("Product created successfully.");
            return new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                ProductImage = product.ProductImage,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                Quantity = product.Quantity,
                Supplier = product.Supplier
            };
        }

        public async Task<bool> DeleteProductAsync(Guid productId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null || product.IsDeleted)
            {
                _loggerService.Error("Product not found or already deleted.");
                throw new KeyNotFoundException("Product not found.");
            }
            await _unitOfWork.Products.SoftRemove(product);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success("Product deleted successfully.");
            return true;
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.Products.GetQueryable()
                .Where(p => !p.IsDeleted)
                .ToListAsync();
            _loggerService.Info($"Retrieved {products.Count} products.");
            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                Category = p.Category,
                Quantity = p.Quantity,
                Supplier = p.Supplier,
            }).ToList();
        }

        public async Task<ProductDetailsDto> GetProductByIdAsync(Guid productId)
        {
            var product = await _unitOfWork.Products.GetQueryable()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
            if (product == null)
            {
                _loggerService.Error("Product not found.");
                throw new KeyNotFoundException("Product not found.");
            }
            _loggerService.Info($"Retrieved product with ID: {productId}");
            return new ProductDetailsDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                CategoryName = product.Category.CategoryName,
                SupplierName = GetUserbySupplierIdAsync(product.SupplierId).Result
            };
        }
        public async Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductDto updateProductDto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null || product.IsDeleted)
            {
                _loggerService.Error("Product not found or already deleted.");
                throw new KeyNotFoundException("Product not found.");
            }
            if (updateProductDto.Quantity != product.Quantity && updateProductDto.Quantity > 0)
            {
                product.Quantity = updateProductDto.Quantity;

            }
            if (updateProductDto.ProductName != null)
            {
                product.ProductName = updateProductDto.ProductName;
            }
            if (updateProductDto.Description != null)
            {
                product.Description = updateProductDto.Description;
            }
            if (updateProductDto.UsageHistory != null)
            {
                product.UsageHistory = updateProductDto.UsageHistory;
            }
            if (updateProductDto.Price > 0)
            {
                product.Price = updateProductDto.Price;
            }
            if (product.CategoryId != updateProductDto.CategoryId)
            {
                product.Category = await GetCategoryByIdAsync(updateProductDto.CategoryId);
                product.CategoryId = updateProductDto.CategoryId;
            }
            if (updateProductDto.ImageFile != null)
                try
                {
                    product.ProductImage = await _blobService.ReplaceImageAsync(
                        updateProductDto.ImageFile.OpenReadStream(),
                        updateProductDto.ImageFile.FileName,
                        product.ProductImage,
                        "blindbox-thumbnails"
                    );
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"[UpdateBlindBoxAsync] ReplaceImageAsync failed: {ex.Message}");
                    throw ErrorHelper.Internal("Upload fails");
                }
            await _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success("Product updated successfully.");
            return new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                Quantity = product.Quantity
            };
        }

        private async Task<string> GetUserbySupplierIdAsync(Guid supplierId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(supplierId);
            if (user == null)
            {
                _loggerService.Error("User not found.");
                throw new KeyNotFoundException("Supplier not found.");
            }
            return user.FullName ?? string.Empty;
        }

        private async Task<Supplier> GetSupplierbyIdAsync(Guid supplierId)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(supplierId);
            if (supplier == null)
            {
                _loggerService.Error("Supplier not found.");
                throw new KeyNotFoundException("Supplier not found.");
            }
            return supplier;
        }

        private async Task<Category> GetCategoryByIdAsync(Guid categoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
            {
                _loggerService.Error("Category not found.");
                throw new KeyNotFoundException("Category not found.");
            }
            return category;
        }
    }
}
