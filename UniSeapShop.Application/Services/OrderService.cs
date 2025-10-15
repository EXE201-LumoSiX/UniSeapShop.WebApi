using Microsoft.AspNetCore.Http;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.CategoryDTOs;
using UniSeapShop.Domain.DTOs.OrderDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Domain.Enums;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class OrderService : IOrderService
{
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _claimsService = claimsService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<OrderDto>> GetPaidOrdersForCustomer()
    {
        var customerId = _claimsService.CurrentUserId;
        _loggerService.Info($"Fetching paid orders for customer with ID: {customerId}");

        var orders = await _unitOfWork.Orders
            .GetAllAsync(o => o.CustomerId == customerId && o.Status == OrderStatus.Completed);

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
                TotalPrice = od.TotalPrice
            }).ToList()
        }).ToList();
    }

    public async Task<List<OrderDto>> GetOrders()
    {
        var customerId = _claimsService.CurrentUserId;
        _loggerService.Info($"Fetching all orders for customer with ID: {customerId}");

        // First get the customer by user ID
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == customerId);
        if (customer == null)
        {
            _loggerService.Error($"Customer not found for user ID: {customerId}");
            throw ErrorHelper.NotFound("Customer not found");
        }

        // Then get all orders for this customer
        var orders = await _unitOfWork.Orders
            .GetAllAsync(o => o.CustomerId == customer.Id);

        _loggerService.Info($"Fetched {orders.Count} orders for customer with ID: {customer.Id}");

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
                ProductName = od.Product?.ProductName ?? "Unknown Product",
                ProductImage = od.Product?.ProductImage,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                TotalPrice = od.TotalPrice
            }).ToList()
        }).ToList();
    }

    public async Task<OrderDto> GetOrderById(Guid id)
    {
        _loggerService.Info($"Fetching order with ID: {id}");

        // Get the current user's ID
        var userId = _claimsService.CurrentUserId;

        // Load the order with its details and customer
        var order = await _unitOfWork.Orders.GetByIdAsync(id, o => o.Customer);

        if (order == null)
        {
            _loggerService.Error($"Order with ID {id} not found");
            throw ErrorHelper.NotFound($"Order with ID {id} not found");
        }

        // Security check: ensure current user owns this order or is an admin
        var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

        if (!isAdmin && order.Customer.UserId != userId)
        {
            _loggerService.Error($"User {userId} attempted to access order {id} belonging to another user");
            throw ErrorHelper.Forbidden("You do not have permission to view this order");
        }

        // Load order details with products
        var orderDetails = await _unitOfWork.OrderDetails.GetAllAsync(
            od => od.OrderId == id,
            od => od.Product);

        _loggerService.Info($"Fetched order {id} with {orderDetails.Count} details");

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            ShipAddress = order.ShipAddress,
            PaymentMethod = order.PaymentMethod,
            Status = order.Status,
            CompletedDate = order.CompletedDate,
            TotalAmount = order.TotalAmount,
            OrderDetails = orderDetails.Select(od => new OrderDetailDto
            {
                Id = od.Id,
                ProductId = od.ProductId,
                ProductName = od.Product?.ProductName ?? "Unknown Product",
                ProductImage = od.Product?.ProductImage,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                TotalPrice = od.TotalPrice
            }).ToList()
        };
    }
    public async Task<List<OrderDto>> GetAllOrderDetails()
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var orderDetails = await _unitOfWork.OrderDetails.GetAllAsync();

        var result = new List<OrderDto>();

        foreach (var order in orders)
        {
            var detailsForOrder = orderDetails
                .Where(od => od.OrderId == order.Id)
                .ToList();

            var detailDtos = new List<OrderDetailDto>();

            foreach (var od in detailsForOrder)
            {
                var productDto = await GetProductDtoAsync(od.ProductId);

                detailDtos.Add(new OrderDetailDto
                {
                    Id = od.Id,
                    ProductId = od.ProductId,
                    ProductName = productDto?.ProductName ?? string.Empty,
                    ProductImage = productDto?.ProductImage ?? string.Empty,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalPrice = od.TotalPrice
                });
            }

            result.Add(new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                ShipAddress = order.ShipAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                CompletedDate = order.CompletedDate,
                TotalAmount = order.TotalAmount,
                OrderDetails = detailDtos
            });
        }

        return result;

        //return orderDetails.Select(od => new OrderDetailDto
        //{
        //    Id = od.Id,
        //    ProductId = od.ProductId,
        //    ProductName = GetProductDtoAsync(od.ProductId).Result.ProductName ?? string.Empty,
        //    ProductImage = GetProductDtoAsync(od.ProductId).Result.ProductImage ?? string.Empty,
        //    Quantity = od.Quantity,
        //    UnitPrice = od.UnitPrice,
        //    TotalPrice = od.TotalPrice
        //}).ToList();
    }

    private async Task<ProductDto?> GetProductDtoAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return new ProductDto
        {
            Id = id,
            ProductName = product.ProductName,
            ProductImage = product.ProductImage,
        };
    }

    public async Task<List<OrderDetailDto>> GetSoldProductsForSupplier()
    {
        var supplierId = _claimsService.CurrentUserId;
        _loggerService.Info($"Fetching sold products for supplier with ID: {supplierId}");

        var orderDetails = await _unitOfWork.OrderDetails
            .GetAllAsync(od => od.Product.SupplierId == supplierId && od.Order.Status == OrderStatus.Completed);

        _loggerService.Info($"Fetched {orderDetails.Count} sold products for supplier with ID: {supplierId}");

        return orderDetails.Select(od => new OrderDetailDto
        {
            Id = od.Id,
            ProductId = od.ProductId,
            ProductName = od.Product.ProductName,
            ProductImage = od.Product.ProductImage,
            Quantity = od.Quantity,
            UnitPrice = od.UnitPrice,
            TotalPrice = od.TotalPrice
        }).ToList();
    }

    public async Task<OrderDto> CreateOrderFromCart(CreateOrderDto createOrderDto)
    {
        var customerId = _claimsService.CurrentUserId;
        _loggerService.Info($"Creating order from cart for customer with ID: {customerId}");

        try
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == customerId);
            if (customer == null)
                throw ErrorHelper.NotFound("Customer not found");

            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
            if (cart == null)
                throw ErrorHelper.NotFound("Cart not found");

            var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            if (!cartItems.Any())
                throw ErrorHelper.BadRequest("No items in cart");

            // Check if any items are already selected (checked)
            var checkedItems = cartItems.Where(ci => ci.IsCheck).ToList();

            if (!checkedItems.Any())
            {
                // If no items are checked, auto-check all items for order creation
                foreach (var item in cartItems)
                {
                    item.IsCheck = true;
                    await _unitOfWork.CartItems.Update(item);
                }

                await _unitOfWork.SaveChangesAsync();
                checkedItems = cartItems.ToList();
            }

            decimal totalAmount = 0;
            var orderDetails = new List<OrderDetail>();

            foreach (var cartItem in checkedItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
                if (product == null)
                    throw ErrorHelper.NotFound($"Product with ID {cartItem.ProductId} not found");

                if (product.Quantity < cartItem.Quantity)
                    throw ErrorHelper.BadRequest(
                        $"Insufficient stock for product {product.ProductName}. Available: {product.Quantity}, Requested: {cartItem.Quantity}");

                var totalPrice = (decimal)product.Price * cartItem.Quantity;
                totalAmount += totalPrice;

                // Update product quantity
                product.Quantity -= cartItem.Quantity;
                await _unitOfWork.Products.Update(product);
            }

            var order = new Order
            {
                CustomerId = customer.Id,
                OrderDate = DateTime.UtcNow,
                ShipAddress = createOrderDto.ShipAddress,
                PaymentMethod = createOrderDto.PaymentGateway.ToString(),
                Status = OrderStatus.Pending,
                Customer = customer
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync(); // Save to get the OrderId

            // Now create order details with the order ID
            foreach (var cartItem in checkedItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
                if (product == null)
                    continue;

                var totalPrice = product.Price * cartItem.Quantity;

                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = totalPrice,
                    Order = order,
                    Product = product
                };

                await _unitOfWork.OrderDetails.AddAsync(orderDetail);
                orderDetails.Add(orderDetail);
            }

            // Remove checked items from cart
            foreach (var checkedItem in checkedItems) await _unitOfWork.CartItems.HardRemoveAsyn(checkedItem);

            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"Order created successfully with ID: {order.Id} for customer: {customer.Id}");

            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                ShipAddress = order.ShipAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                CompletedDate = order.CompletedDate,
                TotalAmount = order.TotalAmount,
                OrderDetails = orderDetails.Select(od => new OrderDetailDto
                {
                    Id = od.Id,
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    ProductImage = od.Product.ProductImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalPrice = od.TotalPrice
                }).ToList()
            };
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error creating order from cart: {e.Message}");
            throw;
        }
    }
}