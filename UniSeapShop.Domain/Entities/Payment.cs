using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentGateway PaymentGateway { get; set; } = PaymentGateway.PayOS;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? GatewayTransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? RedirectUrl { get; set; }
    public string? GatewayResponse { get; set; }

    // Navigation properties
    public required Order Order { get; set; }
}