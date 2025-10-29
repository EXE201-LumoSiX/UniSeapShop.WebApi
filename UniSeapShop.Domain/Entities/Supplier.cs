namespace UniSeapShop.Domain.Entities;

public class Supplier : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string? Location { get; set; } = string.Empty;
    public float Rating { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string AccountBank { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    // Navigation properties
    public required User User { get; set; }
    public List<Product> Products { get; set; } = new();
}