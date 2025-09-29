namespace UniSeapShop.Domain.Entities;

public class OrderDetail : BaseEntity
{
    public Guid OrderId { get; set; } // Corrected from OrdeId to OrderId
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }
    public Guid? VoucherId { get; set; } // Making nullable as it may not always have a voucher
    
    // Navigation properties
    public required Order Order { get; set; }
    public required Product Product { get; set; }
    public Voucher? Voucher { get; set; }
    public Feeback? Feeback { get; set; }
}