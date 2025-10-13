using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.OrderDTOs;

namespace UniSeapShop.API.Controllers;

/// <summary>
///     Controller for managing orders.
/// </summary>
[Route("api/orders")]
[ApiController]
[Authorize] // Require authentication for all endpoints
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    ///     Get all paid orders for the current customer.
    /// </summary>
    /// <returns>List of paid orders.</returns>
    /// <response code="200">Returns the list of paid orders.</response>
    /// <response code="401">Unauthorized if the user is not authenticated.</response>
    [HttpGet("customer/paid-orders")]
    [Authorize(Roles = "User")] // Only accessible by customers
    [ProducesResponseType(typeof(ApiResult<List<OrderDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetPaidOrdersForCustomer()
    {
        try
        {
            var orders = await _orderService.GetPaidOrdersForCustomer();
            return Ok(ApiResult<List<OrderDto>>.Success(orders, "200", "Fetched paid orders successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<OrderDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get all sold products for the current supplier.
    /// </summary>
    /// <returns>List of sold products.</returns>
    /// <response code="200">Returns the list of sold products.</response>
    /// <response code="401">Unauthorized if the user is not authenticated.</response>
    [HttpGet("supplier/sold-products")]
    [Authorize(Roles = "User")] // Only accessible by suppliers
    [ProducesResponseType(typeof(ApiResult<List<OrderDetailDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSoldProductsForSupplier()
    {
        try
        {
            var soldProducts = await _orderService.GetSoldProductsForSupplier();
            return Ok(ApiResult<List<OrderDetailDto>>.Success(soldProducts, "200",
                "Fetched sold products successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<OrderDetailDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Create an order from the current user's cart.
    /// </summary>
    /// <param name="createOrderDto">Order creation data including ship address.</param>
    /// <returns>Created order information.</returns>
    /// <response code="200">Returns the created order.</response>
    /// <response code="400">Bad request if cart is empty or insufficient stock.</response>
    /// <response code="401">Unauthorized if the user is not authenticated.</response>
    /// <response code="404">Not found if customer or cart not found.</response>
    [HttpPost]
    [Authorize(Roles = "User")] // Only accessible by customers
    [ProducesResponseType(typeof(ApiResult<OrderDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        try
        {
            var order = await _orderService.CreateOrderFromCart(createOrderDto);
            return Ok(ApiResult<OrderDto>.Success(order, "200", "Order created successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<OrderDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}