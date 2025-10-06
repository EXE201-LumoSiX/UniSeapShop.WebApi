using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSeapShop.Domain.DTOs.CartItemDTOs;

namespace UniSeapShop.Domain.DTOs.CardDTOs
{
    public class CartDto
    {
        public Guid CartId { get; set; }
        public Guid UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public double TotalPrice { get; set; }
    }
}
