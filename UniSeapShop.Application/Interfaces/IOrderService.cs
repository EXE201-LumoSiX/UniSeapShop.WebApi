using UniSeapShop.Domain.DTOs.OrderDTOs;

namespace UniSeapShop.Application.Interfaces;

public interface IOrderService
{
    Task<List<OrderDto>> GetPaidOrdersForCustomer();
    Task<List<OrderDetailDto>> GetSoldProductsForSupplier();
    Task<OrderDto> CreateOrderFromCart(CreateOrderDto createOrderDto);
    Task<List<OrderDto>> GetOrders();
    Task<List<OrderDto>> GetAllOrderDetails();
    Task<OrderDto> GetOrderById(Guid id);
}