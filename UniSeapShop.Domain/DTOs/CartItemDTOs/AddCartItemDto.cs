namespace UniSeapShop.Domain.DTOs.CartItemDTOs;

public class AddCartItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}