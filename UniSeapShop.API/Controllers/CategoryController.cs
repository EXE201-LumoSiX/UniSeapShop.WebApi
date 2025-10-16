using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.CategoryDTOs;

namespace UniSeapShop.API.Controllers;

/// <summary>
///     Controller for managing categories.
/// </summary>
[Route("api/categories")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    ///     Create a new category.
    /// </summary>
    /// <param name="dto">The category data to create.</param>
    /// <returns>The created category.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var result = await _categoryService.CreateCategoryAsync(dto);
            return Ok(ApiResult<CategoryDto>.Success(result, "201", "Category created successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CategoryDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get all categories.
    /// </summary>
    /// <returns>A list of categories.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        try
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResult<List<CategoryDto>>.Success(result, "200", "Categories retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<CategoryDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get a category by its ID.
    /// </summary>
    /// <param name="id">The ID of the category.</param>
    /// <returns>The category details.</returns>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        try
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(ApiResult<CategoryDetailsDto>.Success(result, "200", "Category retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CategoryDetailsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Update an existing category.
    /// </summary>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="dto">The updated category data.</param>
    /// <returns>The updated category.</returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            var result = await _categoryService.UpdateCategoryAsync(id, dto);
            return Ok(ApiResult<CategoryDto>.Success(result, "200", "Category updated successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CategoryDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Delete a category by its ID.
    /// </summary>
    /// <param name="id">The ID of the category to delete.</param>
    /// <returns>A success message.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            return Ok(ApiResult<bool>.Success(result, "200", "Category deleted successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}