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
        Task<CartDto> AddItemToCartAsync(AddCartItemDto addItemDto);
        Task<CartDto> UpdateItemQuantityAsync(UpdateCartItemDto updateItemDto);
        Task<CartDto> RemoveItemFromCartAsync(Guid productId);
        Task<CartDto> RemoveAllItemsByCustomerIdAsync();
    }
}
