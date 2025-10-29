namespace UniSeapShop.Domain.DTOs.CartItemDTOs;

public class CartItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public bool IsCheck { get; set; }
}