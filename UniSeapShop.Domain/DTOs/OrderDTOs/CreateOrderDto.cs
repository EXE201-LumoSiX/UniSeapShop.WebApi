using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.DTOs.OrderDTOs;

public class CreateOrderDto
{
    public string ShipAddress { get; set; } = string.Empty;
    public PaymentGateway PaymentGateway { get; set; } = PaymentGateway.PayOS;
}