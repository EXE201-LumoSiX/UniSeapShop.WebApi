using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.Entities;

public class Product : BaseEntity
{
    public required string ProductName { get; set; }
    public string ProductImage { get; set; } = string.Empty; // Main image (legacy field)
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public double? OriginalPrice { get; set; } // Original retail price for comparison
    public int Quantity { get; set; }
    public Guid SupplierId { get; set; }
    
    // Second-hand specific properties
    public ProductCondition Condition { get; set; } = ProductCondition.Good;
    public string UsageHistory { get; set; } = string.Empty; // Brief history of the item
    public int? EstimatedAge { get; set; } // Approximate age in months/years
    public string Brand { get; set; } = string.Empty;
    public double? Weight { get; set; } // In kg or appropriate unit
    public string Dimensions { get; set; } = string.Empty; // Format: "LxWxH" in cm or appropriate unit
    
    // Navigation properties
    public required Category Category { get; set; }
    public required Supplier Supplier { get; set; }
    public List<OrderDetail> OrderDetails { get; set; } = new();
    public List<CartItem> CartItems { get; set; } = new();
    public List<ProductImage> Images { get; set; } = new();
}