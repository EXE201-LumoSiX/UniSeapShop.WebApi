namespace UniSeapShop.Domain.DTOs.PaymentDTOs;

public class PaymentInfoDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentGateway { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}