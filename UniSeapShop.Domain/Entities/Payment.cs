namespace UniSeapShop.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;

    // Navigation properties
    public required Order Order { get; set; }
}