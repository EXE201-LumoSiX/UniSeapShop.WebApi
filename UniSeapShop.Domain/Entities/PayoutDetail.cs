namespace UniSeapShop.Domain.Entities;

public class PayoutDetail : BaseEntity
{
    public Guid ReceiverId { get; set; } // Supplier ID who receives the payment
    public double TotalPrice { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double ActualReceipt { get; set; }

    // Navigation properties
    public required Order Order { get; set; }
}