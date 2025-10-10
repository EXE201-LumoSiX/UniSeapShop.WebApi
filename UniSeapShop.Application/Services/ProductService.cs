using Microsoft.EntityFrameworkCore;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.CategoryDTOs;
using UniSeapShop.Domain.DTOs.ProductDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class ProductService : IProductService
{
    private readonly IBlobService _blobService;
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork, ILoggerService loggerService, IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _blobService = blobService;
    }

    public async Task<ProductDto> CreateProductAsunc(CreateProductDto createProductDto)
    {
        // Tính toán giá sau khi giảm giá
        double Price = 0;
        if (createProductDto.Discount == 0)
            Price = createProductDto.OriginalPrice;
        else
            Price = createProductDto.OriginalPrice - createProductDto.Discount / 100 * createProductDto.OriginalPrice;

        // Xử lý upload hình ảnh nếu có
        var productImageUrl = string.Empty;
        if (createProductDto.ProductImageFile != null && createProductDto.ProductImageFile.Length > 0)
            try
            {
                // Tạo tên file duy nhất dựa trên guid
                var fileExtension = Path.GetExtension(createProductDto.ProductImageFile.FileName);
                var fileName = $"products/{Guid.NewGuid()}{fileExtension}";

                // Upload ảnh lên MinIO
                using var stream = createProductDto.ProductImageFile.OpenReadStream();
                await _blobService.UploadFileAsync(fileName, stream);

                // Lấy URL xem trước của ảnh
                productImageUrl = await _blobService.GetPreviewUrlAsync(fileName);
                _loggerService.Success($"Product image uploaded successfully: {fileName}");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error uploading product image: {ex.Message}");
                throw ErrorHelper.Internal("Không thể tải lên hình ảnh sản phẩm.");
            }

        var product = new Product
        {
            ProductName = createProductDto.ProductName,
            Description = createProductDto.Description,
            OriginalPrice = createProductDto.OriginalPrice,
            ProductImage = productImageUrl, // Lưu URL hình ảnh
            UsageHistory = createProductDto.UsageHistory,
            Price = Price,
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
            CategoryName = GetCategoryByIdAsync(product.CategoryId).Result.CategoryName,
            Quantity = product.Quantity,
            SupplierName = GetUserbySupplierIdAsync(product.SupplierId).Result
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
            CategoryName = GetCategoryByIdAsync(p.CategoryId).Result.CategoryName,
            Quantity = p.Quantity,
            SupplierName = GetUserbySupplierIdAsync(p.SupplierId).Result
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

        product.ProductName = updateProductDto.ProductName;
        product.Description = updateProductDto.Description;
        product.Price = updateProductDto.Price;
        product.Quantity = updateProductDto.Quantity;
        if (product.CategoryId != updateProductDto.CategoryId)
        {
            product.Category = await GetCategoryByIdAsync(updateProductDto.CategoryId);
            product.CategoryId = updateProductDto.CategoryId;
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
            CategoryName = GetCategoryByIdAsync(product.CategoryId).Result.CategoryName,
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
        if (supplier == null || supplier.User == null)
        {
            //var user = await _unitOfWork.Users.GetByIdAsync(supplierId);
            //supplier.User = user;
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