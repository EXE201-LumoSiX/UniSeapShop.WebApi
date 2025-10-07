using Microsoft.EntityFrameworkCore;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.CardDTOs;
using UniSeapShop.Domain.DTOs.CartItemDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;
        private readonly IClaimsService _claimsService;

        public CartService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }

        private async Task<CartDto> MapToDto(Cart cart, Guid userId, IEnumerable<CartItem>? cartItems = null, Dictionary<Guid, Product>? productDict = null)
        {
            // Lấy danh sách cartItems nếu chưa được truyền vào
            if (cartItems == null)
            {
                cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            }

            // Lấy thông tin sản phẩm nếu chưa được truyền vào
            if (productDict == null)
            {
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                var products = await _unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.Id));
                productDict = products.ToDictionary(p => p.Id, p => p);
            }

            // Tạo CartDto
            var cartDto = new CartDto
            {
                CartId = cart.Id,
                UserId = userId,
                Items = cartItems.Select(ci =>
                {
                    productDict.TryGetValue(ci.ProductId, out var product);

                    return new CartItemDto
                    {
                        ProductId = ci.ProductId,
                        ProductName = product?.ProductName ?? "Unknown",
                        Price = product?.Price ?? 0,
                        Quantity = ci.Quantity,
                        IsCheck = ci.IsCheck
                    };
                }).ToList(),
                TotalPrice = cartItems.Sum(ci =>
                {
                    productDict.TryGetValue(ci.ProductId, out var product);
                    return (product?.Price ?? 0) * ci.Quantity;
                })
            };

            return cartDto;
        }

        /// <summary>
        /// Lấy thông tin giỏ hàng của người dùng hiện tại
        /// </summary>
        /// <returns>Thông tin giỏ hàng dạng CartDto</returns>
        public async Task<CartDto> GetCartByUserIdAsync()
        {
            var userId = _claimsService.CurrentUserId;

            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                _loggerService.Warn($"Customer not found for user {userId}");
                throw ErrorHelper.NotFound("Customer not found.");
            }

            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
            if (cart == null)
            {
                _loggerService.Warn($"Cart not found for user {userId}");
                throw ErrorHelper.NotFound("Cart not found.");
            }

            var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var products = await _unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.Id));
            var productDict = products.ToDictionary(p => p.Id, p => p);

            var cartDto = await MapToDto(cart, userId, cartItems, productDict);

            _loggerService.Info($"Cart retrieved for user {userId}");
            return cartDto;
        }

        public async Task<CartDto> AddItemToCartAsync(AddCartItemDto addItemDto)
        {
            var userId = _claimsService.CurrentUserId;
            
            // Tìm giỏ hàng hiện có hoặc tạo mới nếu chưa có
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(cu => cu.UserId == userId);
            if (customer == null)
            {
                _loggerService.Warn($"Customer not found for user {userId}");
                throw ErrorHelper.NotFound("Customer not found.");
            }

            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
            if (cart == null)
            {
                // Tạo giỏ hàng mới nếu chưa có
                cart = new Cart { CustomerId = customer.Id, Customer = customer };
                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
            }

            var product = await _unitOfWork.Products.GetByIdAsync(addItemDto.ProductId);
            if (product == null || product.Quantity < addItemDto.Quantity)
            {
                _loggerService.Warn($"Product {addItemDto.ProductId} not available or insufficient stock.");
                throw ErrorHelper.BadRequest("Product not available or insufficient stock.");
            }

            // Lấy danh sách cartItems hiện tại
            var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            var cartItem = cartItems.FirstOrDefault(ci => ci.ProductId == addItemDto.ProductId);

            if (cartItem != null)
            {
                // Cập nhật số lượng nếu sản phẩm đã có trong giỏ
                cartItem.Quantity += addItemDto.Quantity;
                await _unitOfWork.CartItems.Update(cartItem);
            }
            else
            {
                // Thêm mới sản phẩm vào giỏ
                var newCartItem = new CartItem
                {
                    ProductId = addItemDto.ProductId,
                    Quantity = addItemDto.Quantity,
                    CartId = cart.Id,
                    Product = product,
                    Cart = cart
                };
                await _unitOfWork.CartItems.AddAsync(newCartItem);
                cartItems = cartItems.Append(newCartItem).ToList();
            }

            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success($"Product {addItemDto.ProductId} added to cart for user {userId}");
            
            // Map trực tiếp sang DTO thay vì gọi lại GetCartByUserIdAsync
            // Chuẩn bị Dictionary sản phẩm
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var products = await _unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.Id));
            var productDict = products.ToDictionary(p => p.Id, p => p);
            
            // Map sang DTO và trả về
            return await MapToDto(cart, userId, cartItems, productDict);
        }

        public async Task<CartDto> UpdateItemQuantityAsync(UpdateCartItemDto updateItemDto)
        {
            var userId = _claimsService.CurrentUserId;

            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                _loggerService.Warn($"Customer not found for user {userId}");
                throw ErrorHelper.NotFound("Customer not found.");
            }

            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
            if (cart == null)
            {
                _loggerService.Warn($"Cart not found for user {userId}");
                throw ErrorHelper.NotFound("Cart not found.");
            }

            var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                ci => ci.CartId == cart.Id && ci.ProductId == updateItemDto.ProductId
            );

            if (cartItem == null)
            {
                _loggerService.Warn($"Product {updateItemDto.ProductId} not found in cart for user {userId}");
                throw ErrorHelper.NotFound("Item not found in cart.");
            }

            if (updateItemDto.Quantity <= 0)
            {
                await _unitOfWork.CartItems.SoftRemove(cartItem);
            }
            else
            {
                cartItem.Quantity = updateItemDto.Quantity;
                await _unitOfWork.CartItems.Update(cartItem);
            }

            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success($"Product {updateItemDto.ProductId} updated in cart for user {userId}");
            
            // Lấy tất cả cart items sau khi cập nhật
            var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            
            // Chuẩn bị Dictionary sản phẩm
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var products = await _unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.Id));
            var productDict = products.ToDictionary(p => p.Id, p => p);
            
            // Map sang DTO và trả về
            return await MapToDto(cart, userId, cartItems, productDict);
        }

        public async Task<CartDto> RemoveItemFromCartAsync(Guid productId)
        {
            var userId = _claimsService.CurrentUserId;

            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                _loggerService.Warn($"Customer not found for user {userId}");
                throw ErrorHelper.NotFound("Customer not found.");
            }

            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
            if (cart == null)
            {
                _loggerService.Warn($"Cart not found for user {userId}");
                throw ErrorHelper.NotFound("Cart not found.");
            }

            var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                ci => ci.CartId == cart.Id && ci.ProductId == productId
            );

            if (cartItem == null)
            {
                _loggerService.Warn($"Product {productId} not found in cart for user {userId}");
                throw ErrorHelper.NotFound("Product not found in cart.");
            }

            await _unitOfWork.CartItems.SoftRemove(cartItem);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"Product {productId} removed from cart for user {userId}");
            
            // Lấy tất cả cart items sau khi xóa
            var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            
            // Chuẩn bị Dictionary sản phẩm nếu còn sản phẩm trong giỏ hàng
            Dictionary<Guid, Product> productDict = new();
            if (cartItems.Any())
            {
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                var products = await _unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.Id));
                productDict = products.ToDictionary(p => p.Id, p => p);
            }
            
            // Map sang DTO và trả về
            return await MapToDto(cart, userId, cartItems, productDict);
        }

        public async Task<CartDto> RemoveAllItemsByCustomerIdAsync()
        {
            var userId = _claimsService.CurrentUserId;

            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                _loggerService.Warn($"Customer not found for user {userId}");
                throw ErrorHelper.NotFound("Customer not found.");
            }

            var cart = await _unitOfWork.Carts
                .GetQueryable()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id);

            if (cart == null)
            {
                _loggerService.Warn($"Cart not found for customer {customer.Id}");
                throw ErrorHelper.NotFound("Cart not found.");
            }

            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                _loggerService.Info($"No items to remove — cart already empty for user {userId}");
                // Sử dụng MapToDto với danh sách rỗng
                return await MapToDto(cart, userId, new List<CartItem>(), new Dictionary<Guid, Product>());
            }

            await _unitOfWork.CartItems.SoftRemoveRange(cart.CartItems);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"All items removed from cart for user {userId}");
            
            // Map sang DTO với danh sách rỗng và trả về
            return await MapToDto(cart, userId, new List<CartItem>(), new Dictionary<Guid, Product>());
        }
    }
}