namespace UniSeapShop.Domain.Entities;

public class ProductImage : BaseEntity
{
    public required Guid ProductId { get; set; }
    public required string ImageUrl { get; set; }
    public bool IsMainImage { get; set; } = false;

    // Navigation property
    public required Product Product { get; set; }
}