using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.Entities;

public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string ShipAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime? CompletedDate { get; set; }
    public string? CancellationReason { get; set; }
    
    // Navigation properties
    public required Customer Customer { get; set; }
    public List<OrderDetail> OrderDetails { get; set; } = new();
    public Payment? Payment { get; set; }
    public PayoutDetail? PayoutDetail { get; set; }
}