using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.DTOs.ProductDTOs;

public class ProductDetailsDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public double Price { get; set; }
    public ProductCondition ProductCondition { get; set; }
    public string CategoryName { get; set; }
    public int Quantity { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; }
}