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
    public string ProductName { get; set; } = string.Empty;
    public double Price { get; set; }
}