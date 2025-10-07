using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.DTOs.PaymentDTOs;

public class CreatePaymentDto
{
    public string ShipAddress { get; set; } = string.Empty;
    public PaymentGateway PaymentGateway { get; set; } = PaymentGateway.PayOS;
}