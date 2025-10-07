using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSeapShop.Domain.DTOs.OrderDTOs;

namespace UniSeapShop.Application.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderDto>> GetPaidOrdersForCustomer();
        Task<List<OrderDetailDto>> GetSoldProductsForSupplier();
    }
}
