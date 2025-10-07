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
    ///     Thêm một sản phẩm vào giỏ hàng của user hiện tại.
    /// </summary>
    /// <param name="dto">Thông tin sản phẩm và số lượng cần thêm.</param>
    /// <returns>Giỏ hàng sau khi thêm.</returns>
    [HttpPost("items")]
    [Authorize]
    public async Task<IActionResult> AddItemToCart([FromBody] AddCartItemDto dto)
    {
        try
        {
            var result = await _cartService.AddItemToCartAsync(dto);
            return Ok(ApiResult<CartDto>.Success(result, "201", "Item added to cart successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Lấy thông tin giỏ hàng hiện tại của user (dựa trên access token).
    /// </summary>
    /// <returns>Thông tin chi tiết giỏ hàng.</returns>
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
    ///     Cập nhật số lượng của một sản phẩm trong giỏ hàng.
    /// </summary>
    /// <param name="dto">Mã sản phẩm và số lượng mới.</param>
    /// <returns>Giỏ hàng sau khi cập nhật.</returns>
    [HttpPut("items")]
    [Authorize]
    public async Task<IActionResult> UpdateItemQuantity([FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var result = await _cartService.UpdateItemQuantityAsync(dto);
            return Ok(ApiResult<CartDto>.Success(result, "200", "Item quantity updated successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Xóa một sản phẩm khỏi giỏ hàng.
    /// </summary>
    /// <param name="productId">ID sản phẩm cần xóa.</param>
    /// <returns>Giỏ hàng sau khi xóa.</returns>
    [HttpDelete("items/{productId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveItemFromCart(Guid productId)
    {
        try
        {
            var result = await _cartService.RemoveItemFromCartAsync(productId);
            return Ok(ApiResult<CartDto>.Success(result, "200", "Item removed from cart successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Xóa toàn bộ sản phẩm trong giỏ hàng của user hiện tại.
    ///     Thường dùng cho thao tác "dọn sạch" sau khi checkout hoặc reset.
    /// </summary>
    [HttpDelete("remove-all")]
    [Authorize]
    public async Task<IActionResult> RemoveAllItemsForCustomer()
    {
        try
        {
            var result = await _cartService.RemoveAllItemsByCustomerIdAsync();
            return Ok(
                ApiResult<CartDto>.Success(result, "200", "All cart items for the customer removed successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Check/uncheck một sản phẩm cụ thể trong giỏ hàng.
    /// </summary>
    /// <param name="dto">Thông tin sản phẩm và trạng thái check.</param>
    /// <returns>Giỏ hàng sau khi cập nhật.</returns>
    [HttpPatch("items/check")]
    [Authorize]
    public async Task<IActionResult> CheckCartItem([FromBody] CheckCartItemDto dto)
    {
        try
        {
            var result = await _cartService.CheckCartItemAsync(dto);
            return Ok(ApiResult<CartDto>.Success(result, "200", "Cart item check status updated successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Check/uncheck tất cả sản phẩm trong giỏ hàng.
    /// </summary>
    /// <param name="dto">Trạng thái check cho tất cả items.</param>
    /// <returns>Giỏ hàng sau khi cập nhật.</returns>
    [HttpPatch("items/check-all")]
    [Authorize]
    public async Task<IActionResult> CheckAllCartItems([FromBody] CheckAllCartItemsDto dto)
    {
        try
        {
            var result = await _cartService.CheckAllCartItemsAsync(dto);
            return Ok(ApiResult<CartDto>.Success(result, "200", "All cart items check status updated successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<CartDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}