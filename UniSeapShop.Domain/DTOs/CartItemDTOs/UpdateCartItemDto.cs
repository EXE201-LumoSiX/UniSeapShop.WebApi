namespace UniSeapShop.Domain.DTOs.CartItemDTOs;

public class UpdateCartItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}