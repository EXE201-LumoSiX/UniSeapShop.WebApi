using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Domain.DTOs.CategoryDTOs;

public class CategoryDetailsDto
{
    public Guid Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<ProductDto> Products { get; set; } = new();
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public double OriginalPrice { get; set; } = 0;
    public string UsageHistory { get; set; } = string.Empty;
    public double Price { get; set; }
    public Category Category { get; set; }
    public int Quantity { get; set; }
    public Supplier Supplier { get; set; }
}