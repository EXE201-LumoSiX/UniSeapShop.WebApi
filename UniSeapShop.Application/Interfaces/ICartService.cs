using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSeapShop.Domain.DTOs.CardDTOs;
using UniSeapShop.Domain.DTOs.CartItemDTOs;

namespace UniSeapShop.Application.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetCartByUserIdAsync();
        Task AddItemToCartAsync(AddCartItemDto addItemDto);
        Task UpdateItemQuantityAsync(UpdateCartItemDto updateItemDto);
        Task RemoveItemFromCartAsync(Guid productId);
        Task RemoveAllItemsByCustomerIdAsync();
    }
}
