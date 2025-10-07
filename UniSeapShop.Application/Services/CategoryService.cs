using Microsoft.EntityFrameworkCore;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Domain.DTOs.CategoryDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;

        public CategoryService(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
        }
        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            try
            {
                var category = new Category
                { CategoryName = createCategoryDto.CategoryName, 
                  CreatedAt = DateTime.UtcNow,
                  IsDeleted = false
                };

                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success("Category created successfully.");
                return new CategoryDto
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error creating category: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(Guid categoryId)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);

                if (category == null || category.IsDeleted)
                {
                    _loggerService.Error("Category not found or already deleted.");
                    throw new KeyNotFoundException("Category not found.");
                }

                await _unitOfWork.Categories.SoftRemove(category);

                _loggerService.Success("Category deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error deleting category: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetQueryable()
                .Where(c => !c.IsDeleted)
                .ToListAsync();

                _loggerService.Info($"Retrieved {categories.Count} categories.");
                return categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    ProductCount = c.Products.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error fetching categories: {ex.Message}");
                throw;
            }
        }

        public async Task<CategoryDetailsDto> GetCategoryByIdAsync(Guid categoryId)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetQueryable()
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

                if (category == null)
                    throw new Exception("Category not found.");

                _loggerService.Info($"Category retrieved successfully: {category.CategoryName}");
                return new CategoryDetailsDto
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                    Products = category.Products.Select(p => new ProductDto
                    {
                        Id = p.Id,
                        ProductName = p.ProductName,
                        Price = p.Price
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error in GetCategoryByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetQueryable()
                    .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

                if (category == null)
                    throw new Exception("Category not found.");

                category.CategoryName = updateCategoryDto.CategoryName;
                category.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"Category updated successfully: {category.CategoryName}");
                return new CategoryDto
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                    ProductCount = category.Products.Count
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error in UpdateCategoryAsync: {ex.Message}");
                throw;
            }
        }
    }
}
