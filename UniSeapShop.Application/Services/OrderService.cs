using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Domain.DTOs.OrderDTOs;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;
        private readonly IClaimsService _claimsService;

        public OrderService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }

        public async Task<List<OrderDto>> GetPaidOrdersForCustomer()
        {
                var customerId = _claimsService.CurrentUserId;
                _loggerService.Info($"Fetching paid orders for customer with ID: {customerId}");

                var orders = await _unitOfWork.Orders
                    .GetAllAsync(o => o.CustomerId == customerId && o.Status == Domain.Enums.OrderStatus.Completed);

                _loggerService.Info($"Fetched {orders.Count} paid orders for customer with ID: {customerId}");

                return orders.Select(o => new OrderDto
                {
                    Id = o.Id,
                    CustomerId = o.CustomerId,
                    OrderDate = o.OrderDate,
                    ShipAddress = o.ShipAddress,
                    PaymentMethod = o.PaymentMethod,
                    Status = o.Status,
                    CompletedDate = o.CompletedDate,
                    TotalAmount = o.TotalAmount,
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailDto
                    {
                        Id = od.Id,
                        ProductId = od.ProductId,
                        ProductName = od.Product.ProductName,
                        ProductImage = od.Product.ProductImage,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice,
                    }).ToList()
                }).ToList();
        }

        public async Task<List<OrderDetailDto>> GetSoldProductsForSupplier()
        {
                var supplierId = _claimsService.CurrentUserId;
                _loggerService.Info($"Fetching sold products for supplier with ID: {supplierId}");

                var orderDetails = await _unitOfWork.OrderDetails
                    .GetAllAsync(od => od.Product.SupplierId == supplierId && od.Order.Status == Domain.Enums.OrderStatus.Completed);

                _loggerService.Info($"Fetched {orderDetails.Count} sold products for supplier with ID: {supplierId}");

                return orderDetails.Select(od => new OrderDetailDto
                {
                    Id = od.Id,
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    ProductImage = od.Product.ProductImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalPrice = od.TotalPrice,
                }).ToList();
        }
    }
}
