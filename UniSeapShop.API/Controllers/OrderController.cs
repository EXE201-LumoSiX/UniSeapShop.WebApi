using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.OrderDTOs;

namespace UniSeapShop.API.Controllers;

[Route("api/orders")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    ///     Xem danh sách đơn hàng đã thanh toán
    /// </summary>
    /// <returns>Danh sách đơn hàng đã thanh toán</returns>
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
    ///     Xem tất cả đơn hàng của người dùng hiện tại
    /// </summary>
    /// <returns>Danh sách tất cả đơn hàng</returns>
    [HttpGet]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(ApiResult<List<OrderDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var orders = await _orderService.GetOrders();
            return Ok(ApiResult<List<OrderDto>>.Success(orders, "200", "Fetched orders successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<OrderDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
    
    /// <summary>
    ///     Xem chi tiết đơn hàng theo ID
    /// </summary>
    /// <param name="id">ID của đơn hàng cần xem</param>
    /// <returns>Chi tiết đơn hàng</returns>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<OrderDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderById(id);
            return Ok(ApiResult<OrderDto>.Success(order, "200", "Fetched order successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<OrderDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Xem sản phẩm đã bán (dành cho nhà cung cấp)
    /// </summary>
    [HttpGet("supplier/sold-products")]
    [Authorize(Roles = "Supplier")] // Only accessible by suppliers
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
    ///     Tạo đơn hàng từ giỏ hàng
    /// </summary>
    /// <param name="createOrderDto">Thông tin đơn hàng bao gồm địa chỉ giao hàng</param>
    /// <returns>Thông tin đơn hàng vừa tạo</returns>
    [HttpPost]
    [Authorize(Roles = "Customer")] // Only accessible by customers
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