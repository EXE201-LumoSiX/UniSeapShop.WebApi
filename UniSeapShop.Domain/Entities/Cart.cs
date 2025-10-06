namespace UniSeapShop.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid CustomerId { get; set; }

    // Navigation properties
    public required Customer Customer { get; set; }
    public List<CartItem> CartItems { get; set; } = new();
}