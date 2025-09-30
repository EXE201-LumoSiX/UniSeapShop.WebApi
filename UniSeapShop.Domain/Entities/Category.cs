namespace UniSeapShop.Domain.Entities;

public class Category : BaseEntity
{
    public required string CategoryName { get; set; }

    // Navigation properties
    public List<Product> Products { get; set; } = new();
}