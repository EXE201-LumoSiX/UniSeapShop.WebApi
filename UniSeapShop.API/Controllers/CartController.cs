using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.CardDTOs;
using UniSeapShop.Domain.DTOs.CartItemDTOs;

namespace UniSeapShop.API.Controllers;

[Route("api/cart")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }
    /// <summary>
    /// Add an item to the cart.
    /// </summary>
    /// <param name="dto">The item to add.</param>
    /// <returns>A success message.</returns>
    [HttpPost("items")]
    [Authorize]
    public async Task<IActionResult> AddItemToCart([FromBody] AddCartItemDto dto)
    {
        try
        {
            await _cartService.AddItemToCartAsync(dto);
            return Ok(ApiResult<bool>.Success(true, "201", "Item added to cart successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Get the current user's cart.
    /// </summary>
    /// <returns>The cart details.</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCart()
    {
        try
        {
            var result = await _cartService.GetCartByUserIdAsync();
            return Ok(ApiResult<CartDto>.Success(result, "200", "Cart retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Update the quantity of an item in the cart.
    /// </summary>
    /// <param name="dto">The item and new quantity.</param>
    /// <returns>A success message.</returns>
    [HttpPut("items")]
    [Authorize]
    public async Task<IActionResult> UpdateItemQuantity([FromBody] UpdateCartItemDto dto)
    {
        try
        {
            await _cartService.UpdateItemQuantityAsync(dto);
            return Ok(ApiResult<bool>.Success(true, "200", "Item quantity updated successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Remove an item from the cart.
    /// </summary>
    /// <param name="productId">The ID of the product to remove.</param>
    /// <returns>A success message.</returns>
    [HttpDelete("items/{productId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveItemFromCart(Guid productId)
    {
        try
        {
            await _cartService.RemoveItemFromCartAsync(productId);
            return Ok(ApiResult<bool>.Success(true, "200", "Item removed from cart successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Remove all cart items for the current customer (useful for admin or forced cleanup).
    /// </summary>
    [HttpDelete("remove-all")]
    [Authorize]
    public async Task<IActionResult> RemoveAllItemsForCustomer()
    {
        try
        {
            await _cartService.RemoveAllItemsByCustomerIdAsync();
            return Ok(ApiResult<bool>.Success(true, "200", "All cart items for the customer removed successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}