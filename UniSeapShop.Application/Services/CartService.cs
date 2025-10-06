using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
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

        public async Task<CartDto> GetCartByUserIdAsync()
        {
            var userId = _claimsService.CurrentUserId;

            try
            {
                var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _loggerService.Warn($"Customer not found for user {userId}");
                    throw new InvalidOperationException("Customer not found.");
                }

                var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
                if (cart == null)
                {
                    _loggerService.Warn($"Cart not found for user {userId}");
                    throw new InvalidOperationException("Cart not found.");
                }

                var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);

                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();

                var products = await _unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.Id));
                var productDict = products.ToDictionary(p => p.Id, p => p);

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

                _loggerService.Info($"Cart retrieved for user {userId}");
                return cartDto;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error retrieving cart for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task AddItemToCartAsync(AddCartItemDto addItemDto)
        {
            var userId = _claimsService.CurrentUserId;
            try
            {
                var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId)
                           ?? new Cart { CustomerId = userId, Customer = await _unitOfWork.Customers.FirstOrDefaultAsync(cu => cu.UserId == userId) };

                if (cart.Id == Guid.Empty) // Ensure the cart is saved
                {
                    await _unitOfWork.Carts.AddAsync(cart);
                    await _unitOfWork.SaveChangesAsync();
                }

                var product = await _unitOfWork.Products.GetByIdAsync(addItemDto.ProductId);
                if (product == null || product.Quantity < addItemDto.Quantity)
                {
                    _loggerService.Warn($"Product {addItemDto.ProductId} not available or insufficient stock.");
                    throw new InvalidOperationException("Product not available or insufficient stock.");
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addItemDto.ProductId);
                if (cartItem != null)
                {
                    cartItem.Quantity += addItemDto.Quantity;
                }
                else
                {
                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = addItemDto.ProductId,
                        Quantity = addItemDto.Quantity,
                        CartId = cart.Id,
                        Product = product,
                        Cart = cart
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"Product {addItemDto.ProductId} added to cart for user {userId}");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error adding product {addItemDto.ProductId} to cart for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateItemQuantityAsync(UpdateCartItemDto updateItemDto)
        {
            var userId = _claimsService.CurrentUserId;

            try
            {
                var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _loggerService.Warn($"Customer not found for user {userId}");
                    throw new InvalidOperationException("Customer not found.");
                }

                var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
                if (cart == null)
                {
                    _loggerService.Warn($"Cart not found for user {userId}");
                    throw new InvalidOperationException("Cart not found.");
                }

                var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.Id && ci.ProductId == updateItemDto.ProductId
                );

                if (cartItem == null)
                {
                    _loggerService.Warn($"Product {updateItemDto.ProductId} not found in cart for user {userId}");
                    throw new InvalidOperationException("Item not found in cart.");
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

                // 5️⃣ Lưu thay đổi
                await _unitOfWork.SaveChangesAsync();
                _loggerService.Success($"Product {updateItemDto.ProductId} updated in cart for user {userId}");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error updating product {updateItemDto.ProductId} in cart for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveItemFromCartAsync(Guid productId)
        {
            var userId = _claimsService.CurrentUserId;

            try
            {
                var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _loggerService.Warn($"Customer not found for user {userId}");
                    throw new InvalidOperationException("Customer not found.");
                }

                var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
                if (cart == null)
                {
                    _loggerService.Warn($"Cart not found for user {userId}");
                    throw new InvalidOperationException("Cart not found.");
                }

                var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                    ci => ci.CartId == cart.Id && ci.ProductId == productId
                );

                if (cartItem == null)
                {
                    _loggerService.Warn($"Product {productId} not found in cart for user {userId}");
                    throw new InvalidOperationException("Product not found in cart.");
                }

                await _unitOfWork.CartItems.SoftRemove(cartItem);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"Product {productId} removed from cart for user {userId}");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error removing product {productId} from cart for user {userId}: {ex.Message}");
                throw;
            }
        }
    }
}