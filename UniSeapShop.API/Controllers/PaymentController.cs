using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.OrderDTOs;
using UniSeapShop.Domain.DTOs.PaymentDTOs;
using UniSeapShop.Domain.Enums;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IClaimsService _claimsService;
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService, IClaimsService claimsService)
    {
        _paymentService = paymentService;
        _claimsService = claimsService;
    }

    /// <summary>
    ///     Get all payments with optional filtering
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllPayments(
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var payments = await _paymentService.GetAllPayments(status, fromDate, toDate);
            return Ok(ApiResult<List<PaymentInfoDto>>.Success(payments, "200", "Payments retrieved successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<PaymentInfoDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Create payment link from current user's cart
    /// </summary>
    [HttpPost("create-link")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentDto createPaymentDto)
    {
        try
        {
            var userId = _claimsService.CurrentUserId;

            var createOrderDto = new CreateOrderDto
            {
                ShipAddress = createPaymentDto.ShipAddress,
                PaymentGateway = createPaymentDto.PaymentGateway
            };

            var paymentUrl = await _paymentService.ProcessPayment(userId, createOrderDto);

            return Ok(ApiResult<string>.Success(paymentUrl, "200", "Payment link created successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<string>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Create payment link for an existing order
    /// </summary>
    /// <param name="orderId">The ID of the order to create payment for</param>
    [HttpPost("create-link/{orderId:guid}")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentLinkForOrder(Guid orderId)
    {
        try
        {
            var paymentUrl = await _paymentService.ProcessPaymentForOrder(orderId);
            return Ok(ApiResult<string>.Success(paymentUrl, "200", "Payment link created successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<string>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get payment status by payment ID
    /// </summary>
    [HttpGet("{paymentId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(Guid paymentId)
    {
        try
        {
            var status = await _paymentService.GetPaymentStatus(paymentId);
            return Ok(ApiResult<PaymentStatusDto>.Success(status, "200", "Payment status retrieved successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<PaymentStatusDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get payment by order code (for client redirect handling)
    /// </summary>
    [HttpGet("order-code/{orderCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPaymentByOrderCode(string orderCode)
    {
        try
        {
            var status = await _paymentService.GetPaymentByOrderCode(orderCode);
            return Ok(ApiResult<PaymentStatusDto>.Success(status, "200", "Payment found successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<PaymentStatusDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get payment status by payment ID (read-only, no sync)
    /// </summary>
    [HttpGet("{paymentId}/status-only")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatusOnly(Guid paymentId)
    {
        try
        {
            var status = await _paymentService.GetPaymentStatusOnly(paymentId);
            return Ok(ApiResult<PaymentStatusDto>.Success(status, "200", "Payment status retrieved successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<PaymentStatusDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Webhook endpoint to receive PayOS payment notifications (POST)
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook([FromBody] WebhookType webhookData)
    {
        try
        {
            await _paymentService.ProcessWebhook(webhookData);
            return Ok();
        }
        catch (Exception)
        {
            return Ok();
        }
    }

    /// <summary>
    ///     Webhook endpoint to receive PayOS payment notifications (GET)
    /// </summary>
    [HttpGet("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhookGet(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode)
    {
        try
        {
            await _paymentService.ProcessWebhookGet(orderCode, status, code);
            return Ok("Webhook processed successfully");
        }
        catch (Exception ex)
        {
            return Ok($"Webhook processing error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Cancel a pending payment
    /// </summary>
    [HttpPost("{paymentId}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelPayment(Guid paymentId, [FromBody] CancelPaymentDto cancelPaymentDto)
    {
        try
        {
            var result = await _paymentService.CancelPayment(paymentId, cancelPaymentDto.Reason);
            return Ok(ApiResult<bool>.Success(result, "200", "Payment cancelled successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Get payment history for a specific order
    /// </summary>
    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrderId(Guid orderId)
    {
        try
        {
            var payments = await _paymentService.GetPaymentsByOrderId(orderId);
            return Ok(ApiResult<List<PaymentStatusDto>>.Success(payments, "200",
                "Order payments retrieved successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<PaymentStatusDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Update order status (for internal use)
    /// </summary>
    [HttpPut("order/{orderId}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] OrderStatus status)
    {
        try
        {
            var order = await _paymentService.UpdateOrderStatus(orderId, status);
            return Ok(ApiResult<OrderDto>.Success(order, "200", "Order status updated successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<OrderDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}