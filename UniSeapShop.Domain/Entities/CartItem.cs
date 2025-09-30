namespace UniSeapShop.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid CartId { get; set; }
    public bool IsCheck { get; set; } = false;

    // Navigation properties
    public required Product Product { get; set; }
    public required Cart Cart { get; set; }
}