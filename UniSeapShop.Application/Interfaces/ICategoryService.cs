using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSeapShop.Domain.DTOs.CategoryDTOs;

namespace UniSeapShop.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDetailsDto> GetCategoryByIdAsync(Guid categoryId);
        Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto updateCategoryDto);
        Task<bool> DeleteCategoryAsync(Guid categoryId);
    }
}
