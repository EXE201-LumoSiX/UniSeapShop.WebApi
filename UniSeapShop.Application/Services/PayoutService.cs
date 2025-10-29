using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.PayoutDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class PayoutService : IPayoutService
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PayoutService(IConfiguration configuration, ILoggerService loggerService, IUnitOfWork unitOfWork, IClaimsService claimsService, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _loggerService = loggerService;
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PayoutDetailsDto> CreatePayout(Guid OrderId)
        {
            _loggerService.Info($"Fetching order with ID: {OrderId}");

            // Get the current user's ID
            var userId = _claimsService.CurrentUserId;
            var supplier = await _unitOfWork.Suppliers.FirstOrDefaultAsync(x => x.UserId == userId);
            if (supplier == null)
            {
                _loggerService.Error($"Supplier with ID {supplier.Id} not found");
                throw ErrorHelper.NotFound($"Supplier with ID {supplier.Id} not found");
                throw ErrorHelper.NotFound($"Supplier with ID {supplier.Id} not found");
            }
            if (string.IsNullOrEmpty(supplier.AccountNumber) || string.IsNullOrEmpty(supplier.AccountName) || string.IsNullOrEmpty(supplier.AccountBank))
            {
                _loggerService.Error($"Supplier with ID {supplier.Id} has not set up payment information");
                throw ErrorHelper.BadRequest($"Please set up payment information before creating a payout");
            }
            var existPayout = await _unitOfWork.PayoutDetails.FirstOrDefaultAsync(x => x.OrderId == OrderId);
            if (existPayout != null)
            {
                _loggerService.Error($"Payout has exist");
                throw ErrorHelper.BadRequest($"Payout has exist");
            }
            var order = await _unitOfWork.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
            var orderDetail = await _unitOfWork.OrdersDetail.FirstOrDefaultAsync(o => o.OrderId == OrderId);
            if (order == null || orderDetail == null)
            {
                _loggerService.Error($"Order with ID {OrderId} not found");
                throw ErrorHelper.NotFound($"Order with ID {OrderId} not found");
            }
            var payout = new PayoutDetail
            {
                ReceiverId = userId,
                OrderId = OrderId,
                Status = "Pending",
                TotalPrice = orderDetail.TotalPrice,
                Order = order,
                Id = new Guid(),
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
                ActualReceipt = orderDetail.TotalPrice
            };
            await _unitOfWork.PayoutDetails.AddAsync(payout);
            await _unitOfWork.SaveChangesAsync();
            return new PayoutDetailsDto
            {
                Id = payout.Id,
                OrderID = payout.OrderId,
                Status = payout.Status,
                RecieverId = payout.ReceiverId,
                TotalPrice = payout.TotalPrice,
            };
        }

        public async Task<List<PayoutDetailsDto>> GetAllPayout()
        {
            var userId = _claimsService.CurrentUserId;
            var supplier = await _unitOfWork.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

            if (!isAdmin)
            {
                _loggerService.Error($"User {userId} do not have permission to view");
                throw ErrorHelper.Forbidden("You do not have permission to view this");
            }
            var payoutDetails = await _unitOfWork.PayoutDetails.GetAllAsync();
            return payoutDetails.Select(p => new PayoutDetailsDto
            {
                Id = p.Id,
                OrderID = p.OrderId,
                Status = p.Status,
                RecieverId = p.ReceiverId,
                TotalPrice = p.TotalPrice
            }).ToList();
        }

        public async Task<PayoutDetailsDto> GetPayoutById(Guid payoutId)
        {
            _loggerService.Info($"Fetching payout with ID: {payoutId}");

            // Get the current user's ID
            var userId = _claimsService.CurrentUserId;

            //

            // Load the order with its details and customer
            var payout = await _unitOfWork.PayoutDetails.FirstOrDefaultAsync(p => p.Id == payoutId);

            if (payout == null)
            {
                _loggerService.Error($"Payout with ID {payoutId} not found");
                throw ErrorHelper.NotFound($"Payout with ID {payoutId} not found");
            }
            var supplier = await _unitOfWork.Suppliers.FirstOrDefaultAsync(c => c.Id == payout.ReceiverId);
            // Security check: ensure current user owns this order or is an admin
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

            if (!isAdmin && payout.ReceiverId != userId)
            {
                _loggerService.Error($"User {userId} attempted to access payout {payoutId} belonging to another user");
                throw ErrorHelper.Forbidden("You do not have permission to view this order");
            }
            return new PayoutDetailsDto
            {
                Id = payoutId,
                OrderID = payout.OrderId,
                Status = payout.Status,
                RecieverId = payout.ReceiverId,
                AccountName = supplier.AccountName,
                AccountNumber = supplier.AccountNumber,
                AccountBank = supplier.AccountBank,
                TotalPrice = payout.TotalPrice
            };
        }
        public async Task<List<PayoutDetailsDto>> GetPayoutForSupplier()
        {
            // Get the current user's ID
            var userId = _claimsService.CurrentUserId;

            // Load the order with its details and customer
            var payout = await _unitOfWork.PayoutDetails.GetAllAsync(p => p.ReceiverId == userId);

            // Security check: ensure current user owns this order or is an admin

            return payout.Select(p => new PayoutDetailsDto
            {
                Id = p.Id,
                OrderID = p.OrderId,
                Status = p.Status,
                RecieverId = p.ReceiverId,
                TotalPrice = p.TotalPrice
            }).ToList();
        }
        public async Task<PayoutDetailsDto> UpdatePayout(Guid payoutId, string status)
        {
            _loggerService.Info($"Fetching payout with ID: {payoutId}");
            var userId = _claimsService.CurrentUserId;

            //
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            var payout = await _unitOfWork.PayoutDetails.FirstOrDefaultAsync(p => p.Id == payoutId);
            // Get the current user's ID
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

            if (!isAdmin)
            {
                _loggerService.Error($"User {userId} attempted to access payout {payoutId} belonging to another user");
                throw ErrorHelper.Forbidden("You do not have permission to view this order");
            }
            payout.Status = status;
            await _unitOfWork.PayoutDetails.Update(payout);
            await _unitOfWork.SaveChangesAsync();
            return new PayoutDetailsDto
            {
                Id = payout.Id,
                OrderID = payout.OrderId,
                Status = payout.Status,
                RecieverId = payout.ReceiverId,
                TotalPrice = payout.TotalPrice,
            };
        }
    }
}
