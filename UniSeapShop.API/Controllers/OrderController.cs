using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.OrderDTOs;

namespace UniSeapShop.API.Controllers;

/// <summary>
/// Controller for managing orders.
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
    /// Get all paid orders for the current customer.
    /// </summary>
    /// <returns>List of paid orders.</returns>
    /// <response code="200">Returns the list of paid orders.</response>
    /// <response code="401">Unauthorized if the user is not authenticated.</response>
    [HttpGet("customer/paid-orders")]
    [Authorize(Roles = "Customer")] // Only accessible by customers
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
    /// Get all sold products for the current supplier.
    /// </summary>
    /// <returns>List of sold products.</returns>
    /// <response code="200">Returns the list of sold products.</response>
    /// <response code="401">Unauthorized if the user is not authenticated.</response>
    [HttpGet("supplier/sold-products")]
    [Authorize(Roles = "Supplier")] // Only accessible by suppliers
    [ProducesResponseType(typeof(ApiResult<List<OrderDetailDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSoldProductsForSupplier()
    {
        try
        {
            var soldProducts = await _orderService.GetSoldProductsForSupplier();
            return Ok(ApiResult<List<OrderDetailDto>>.Success(soldProducts, "200", "Fetched sold products successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<OrderDetailDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}