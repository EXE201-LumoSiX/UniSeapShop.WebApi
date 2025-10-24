using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.PaymentDTOs;
using UniSeapShop.Domain.DTOs.PayoutDTOs;

namespace UniSeapShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayoutController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        public PayoutController(IPayoutService payoutService)
        {
            _payoutService = payoutService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var payout = await _payoutService.GetAllPayout();
                return Ok(ApiResult<List<PayoutDetailsDto>>.Success(payout, "200", "Payments retrieved successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<List<PaymentInfoDto>>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentbyId(Guid id)
        {
            try
            {
                var payout = await _payoutService.GetPayoutById(id);
                return Ok(ApiResult<PayoutDetailsDto>.Success(payout, "200", "Payments retrieved successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<List<PaymentInfoDto>>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePayout([FromBody] Guid orderId)
        {
            try
            {
                var payout = await _payoutService.CreatePayout(orderId);
                return Ok(ApiResult<PayoutDetailsDto>.Success(payout, "200", "Payments retrieved successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<List<PaymentInfoDto>>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdatePayout(Guid payoutId, string status)
        {
            try
            {
                var payout = await _payoutService.UpdatePayout(payoutId, status);
                return Ok(ApiResult<PayoutDetailsDto>.Success(payout, "200", "Payments retrieved successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<List<PaymentInfoDto>>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
