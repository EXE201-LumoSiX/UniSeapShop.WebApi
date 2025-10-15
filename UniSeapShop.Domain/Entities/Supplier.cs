namespace UniSeapShop.Domain.Entities;

public class Supplier : BaseEntity
{
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public float Rating { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public required User User { get; set; }
    public List<Product> Products { get; set; } = new();
}