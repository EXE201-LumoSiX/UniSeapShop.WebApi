namespace UniSeapShop.Domain.DTOs.PaymentDTOs;

public class PaymentStatusDto
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}