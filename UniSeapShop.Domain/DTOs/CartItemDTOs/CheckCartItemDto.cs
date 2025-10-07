namespace UniSeapShop.Domain.DTOs.CartItemDTOs;

public class CheckCartItemDto
{
    public Guid ProductId { get; set; }
    public bool IsCheck { get; set; }
}

public class CheckAllCartItemsDto
{
    public bool IsCheckAll { get; set; }
}