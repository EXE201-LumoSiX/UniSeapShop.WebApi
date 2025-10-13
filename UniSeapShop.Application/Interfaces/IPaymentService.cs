using Net.payOS.Types;
using UniSeapShop.Domain.DTOs.OrderDTOs;
using UniSeapShop.Domain.DTOs.PaymentDTOs;
using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Application.Interfaces;

public interface IPaymentService
{
    Task<List<PaymentInfoDto>> GetAllPayments(PaymentStatus? status = null, DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<string> ProcessPayment(Guid userId, CreateOrderDto createOrderDto);
    Task<string> ProcessPaymentForOrder(Guid orderId);
    Task ProcessWebhook(WebhookType webhookData);
    Task ProcessWebhookGet(long orderCode, string status, string code);
    Task<PaymentStatusDto> GetPaymentStatus(Guid paymentId);
    Task<PaymentStatusDto> GetPaymentStatusOnly(Guid paymentId);
    Task<PaymentStatusDto> GetPaymentByOrderCode(string orderCode);
    Task<bool> CancelPayment(Guid paymentId, string reason);
    Task<List<PaymentStatusDto>> GetPaymentsByOrderId(Guid orderId);
    Task<OrderDto> CreateOrderFromCart(Guid customerId, CreateOrderDto createOrderDto);
    Task<OrderDto> UpdateOrderStatus(Guid orderId, OrderStatus status);
}