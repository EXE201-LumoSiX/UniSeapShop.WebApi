using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.DTOs.OrderDTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public string ShipAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CancellationReason { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderDetailDto> OrderDetails { get; set; } = new();
}