namespace UniSeapShop.Domain.Entities;

public class Voucher : BaseEntity
{
    public required string Code { get; set; }
    public double DiscountPercent { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public List<OrderDetail> OrderDetails { get; set; } = new();
}