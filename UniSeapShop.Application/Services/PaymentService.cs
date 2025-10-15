using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.OrderDTOs;
using UniSeapShop.Domain.DTOs.PaymentDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Domain.Enums;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IClaimsService _claimsService;
    private readonly IConfiguration _configuration;
    private readonly ILoggerService _loggerService;
    private readonly PayOS _payOs;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(PayOS payOs, IUnitOfWork unitOfWork, ILoggerService loggerService,
        IClaimsService claimsService, IConfiguration configuration)
    {
        _payOs = payOs;
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _claimsService = claimsService;
        _configuration = configuration;
    }

    public async Task<List<PaymentInfoDto>> GetAllPayments(PaymentStatus? status = null, DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var query = _unitOfWork.Payments.GetQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(p => p.CreatedAt <= toDate.Value);

            query = query.OrderByDescending(p => p.CreatedAt);

            var payments = await query.ToListAsync();

            return payments.Select(p => new PaymentInfoDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                Amount = p.Amount,
                PaymentGateway = p.PaymentGateway.ToString(),
                Status = p.Status.ToString(),
                GatewayTransactionId = p.GatewayTransactionId,
                PaymentUrl = p.PaymentUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error getting payments: {e.Message}");
            throw;
        }
    }

    public async Task<string> ProcessPayment(Guid userId, CreateOrderDto createOrderDto)
    {
        _loggerService.Info($"[PAYMENT] Starting payment process for UserId: {userId}");
        try
        {
            // Get customer by user ID
            _loggerService.Info($"[PAYMENT] Phase 1: Finding customer for UserId: {userId}");
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                _loggerService.Error($"[PAYMENT] Phase 1 FAILED: Customer not found for UserId: {userId}");
                throw ErrorHelper.NotFound("Customer not found");
            }

            _loggerService.Info($"[PAYMENT] Phase 1 SUCCESS: Customer found - CustomerId: {customer.Id}");

            _loggerService.Info($"[PAYMENT] Phase 2: Creating order from cart for CustomerId: {customer.Id}");
            var order = await CreateOrderFromCart(customer.Id, createOrderDto);
            _loggerService.Info(
                $"[PAYMENT] Phase 2 SUCCESS: Order created - OrderId: {order.Id}, TotalAmount: {order.TotalAmount}");

            // Get redirect URL from configuration or use default
            _loggerService.Info("[PAYMENT] Phase 3: Configuring URLs and creating payment record");
            var defaultRedirectUrl = _configuration["PaymentSettings:SuccessUrl"]
                                     ?? Environment.GetEnvironmentVariable("PAYMENT_SUCCESS_URL")
                                     ?? "https://uni-seap-shop-web-app.vercel.app/success-payment";

            var cancelUrl = _configuration["PaymentSettings:CancelUrl"]
                            ?? Environment.GetEnvironmentVariable("PAYMENT_CANCEL_URL")
                            ?? "https://uni-seap-shop-web-app.vercel.app/failed-payment";

            // Get webhook URL from configuration  
            var webhookUrl = _configuration["PaymentSettings:WebhookUrl"]
                             ?? Environment.GetEnvironmentVariable("PAYMENT_WEBHOOK_URL")
                             ?? "https://uniseapshop.fpt-devteam.fun/api/payments/webhook";

            _loggerService.Info(
                $"[PAYMENT] Phase 3: URLs configured - RedirectUrl: {defaultRedirectUrl}, CancelUrl: {cancelUrl}, WebhookUrl: {webhookUrl}");

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentGateway = PaymentGateway.PayOS,
                Status = PaymentStatus.Pending,
                RedirectUrl = defaultRedirectUrl,
                Order = await _unitOfWork.Orders.GetByIdAsync(order.Id) ?? throw ErrorHelper.NotFound("Order not found")
            };

            _loggerService.Info($"[PAYMENT] Phase 3: Payment record created - Amount: {payment.Amount}");
            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Info(
                $"[PAYMENT] Phase 3 SUCCESS: Payment record saved to database - PaymentId: {payment.Id}");

            _loggerService.Info("[PAYMENT] Phase 4: Preparing PayOS payment data");
            var itemList = new List<ItemData>();
            foreach (var detail in order.OrderDetails)
                itemList.Add(new ItemData(
                    detail.ProductName ?? "Product",
                    detail.Quantity,
                    (int)detail.UnitPrice
                ));
            _loggerService.Info($"[PAYMENT] Phase 4: Item list prepared - {itemList.Count} items");

            var orderCode = DateTime.Now.Ticks % 1000000000; // Simple order code generation

            // PayOS requires description max 25 chars, so use first 5 chars of order GUID
            var shortOrderId = order.Id.ToString().Substring(0, 5);
            var paymentDescription = $"Uniseap payment #{shortOrderId}";

            _loggerService.Info(
                $"[PAYMENT] Phase 4: PayOS data configured - OrderCode: {orderCode}, Description: {paymentDescription}");

            var paymentData = new PaymentData(
                orderCode,
                (int)order.TotalAmount,
                paymentDescription,
                itemList,
                defaultRedirectUrl, // Success return URL
                cancelUrl // Cancel return URL
            );

            // PayOS sẽ gọi webhook URL sau khi payment thành công
            _loggerService.Info(
                $"[PAYMENT] Phase 4: PaymentData created with Return URLs - Success: {defaultRedirectUrl}, Cancel: {cancelUrl}");
            _loggerService.Info(
                $"[PAYMENT] Phase 4: Webhook URL for server notifications: {webhookUrl} (configured in PayOS merchant settings)");

            _loggerService.Info("[PAYMENT] Phase 5: Calling PayOS createPaymentLink API");
            var paymentResult = await _payOs.createPaymentLink(paymentData);
            _loggerService.Info(
                $"[PAYMENT] Phase 5 SUCCESS: PayOS API response received - CheckoutUrl: {paymentResult.checkoutUrl}");

            _loggerService.Info("[PAYMENT] Phase 6: Updating payment record with PayOS response");
            payment.GatewayTransactionId = paymentResult.orderCode.ToString();
            payment.PaymentUrl = paymentResult.checkoutUrl;
            payment.GatewayResponse = JsonConvert.SerializeObject(paymentResult);

            await _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Info(
                $"[PAYMENT] Phase 6 SUCCESS: Payment record updated with GatewayTransactionId: {payment.GatewayTransactionId}");

            _loggerService.Info(
                $"[PAYMENT] PROCESS COMPLETED SUCCESSFULLY - OrderId: {order.Id}, PaymentId: {payment.Id}, CheckoutUrl: {paymentResult.checkoutUrl}");

            return paymentResult.checkoutUrl;
        }
        catch (Exception ex)
        {
            _loggerService.Error(
                $"[PAYMENT] PROCESS FAILED - UserId: {userId}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            throw ErrorHelper.BadRequest("Unable to create payment link. Please try again later.");
        }
    }

    public async Task<string> ProcessPaymentForOrder(Guid orderId)
    {
        _loggerService.Info($"[PAYMENT_FOR_ORDER] Starting payment process for OrderId: {orderId}");
        try
        {
            // Validation: Check if orderId is valid GUID
            if (orderId == Guid.Empty)
            {
                _loggerService.Error("[PAYMENT_FOR_ORDER] Invalid OrderId: Empty GUID");
                throw ErrorHelper.BadRequest("Invalid order ID");
            }

            _loggerService.Info($"[PAYMENT_FOR_ORDER] Phase 1: Finding order with OrderId: {orderId}");

            // Load the order using the repository
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId, o => o.Customer);
            if (order == null)
            {
                _loggerService.Error($"[PAYMENT_FOR_ORDER] Phase 1 FAILED: Order not found for OrderId: {orderId}");
                throw ErrorHelper.NotFound("Order not found");
            }

            // Validation: Check if order already has a completed or pending payment
            var existingPayments = await _unitOfWork.Payments.GetAllAsync(p => p.OrderId == orderId);
            if (existingPayments != null && existingPayments.Any())
            {
                var completedPayment = existingPayments.FirstOrDefault(p => p.Status == PaymentStatus.Completed);
                if (completedPayment != null)
                {
                    _loggerService.Error($"[PAYMENT_FOR_ORDER] Order {orderId} already has a completed payment - PaymentId: {completedPayment.Id}");
                    throw ErrorHelper.BadRequest("This order has already been paid");
                }

                var pendingPayment = existingPayments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
                if (pendingPayment != null)
                {
                    _loggerService.Warn($"[PAYMENT_FOR_ORDER] Order {orderId} already has a pending payment - PaymentId: {pendingPayment.Id}");
                    throw ErrorHelper.BadRequest($"This order already has a pending payment. Please complete or cancel the existing payment first.");
                }
            }

            // Load order details separately with product information
            var orderDetails = await _unitOfWork.OrderDetails.GetAllAsync(
                od => od.OrderId == orderId,
                od => od.Product);

            if (orderDetails != null && orderDetails.Any())
            {
                // Assign the loaded order details to the order
                order.OrderDetails = orderDetails;
                _loggerService.Info($"[PAYMENT_FOR_ORDER] Loaded {orderDetails.Count} order details with products");
            }
            else
            {
                _loggerService.Error($"[PAYMENT_FOR_ORDER] No order details found for OrderId: {orderId}");
                throw ErrorHelper.BadRequest("Order has no items");
            }

            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 1 SUCCESS: Order found - OrderId: {order.Id}, TotalAmount: {order.TotalAmount}");

            // Validation: Check if order is in correct status for payment
            if (order.Status != OrderStatus.Pending)
            {
                _loggerService.Error(
                    $"[PAYMENT_FOR_ORDER] Order status is not Pending. Current status: {order.Status}");
                throw ErrorHelper.BadRequest($"Order is not available for payment. Current status: {order.Status}");
            }

            // Validation: Check if order has valid total amount
            if (order.TotalAmount <= 0)
            {
                _loggerService.Error($"[PAYMENT_FOR_ORDER] Invalid order total amount: {order.TotalAmount}");
                throw ErrorHelper.BadRequest("Order total amount must be greater than zero");
            }

            // Validation: Check if customer exists
            if (order.Customer == null)
            {
                _loggerService.Error($"[PAYMENT_FOR_ORDER] Customer not found for order {orderId}");
                throw ErrorHelper.BadRequest("Customer information is missing");
            }

            // Get redirect URL from configuration or use default
            _loggerService.Info("[PAYMENT_FOR_ORDER] Phase 2: Configuring URLs and creating payment record");
            var defaultRedirectUrl = _configuration["PaymentSettings:SuccessUrl"]
                                     ?? Environment.GetEnvironmentVariable("PAYMENT_SUCCESS_URL")
                                     ?? "https://uniseapshop.vercel.app/payment-success";

            var cancelUrl = _configuration["PaymentSettings:CancelUrl"]
                            ?? Environment.GetEnvironmentVariable("PAYMENT_CANCEL_URL")
                            ?? "https://uniseapshop.vercel.app/payment-cancel";

            // Get webhook URL from configuration  
            var webhookUrl = _configuration["PaymentSettings:WebhookUrl"]
                             ?? Environment.GetEnvironmentVariable("PAYMENT_WEBHOOK_URL")
                             ?? "https://uniseapshop.fpt-devteam.fun/api/payments/webhook";

            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 2: URLs configured - RedirectUrl: {defaultRedirectUrl}, CancelUrl: {cancelUrl}, WebhookUrl: {webhookUrl}");

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentGateway = PaymentGateway.PayOS,
                Status = PaymentStatus.Pending,
                RedirectUrl = defaultRedirectUrl,
                Order = order
            };

            _loggerService.Info($"[PAYMENT_FOR_ORDER] Phase 2: Payment record created - Amount: {payment.Amount}");
            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 2 SUCCESS: Payment record saved to database - PaymentId: {payment.Id}");

            _loggerService.Info("[PAYMENT_FOR_ORDER] Phase 3: Preparing PayOS payment data");
            var itemList = new List<ItemData>();

            // Safely process OrderDetails with proper null checks
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                _loggerService.Info($"[PAYMENT_FOR_ORDER] Processing {order.OrderDetails.Count} items for payment");

                foreach (var detail in order.OrderDetails)
                {
                    if (detail == null)
                    {
                        _loggerService.Error("[PAYMENT_FOR_ORDER] Skipping null order detail");
                        continue;
                    }

                    if (detail.Product != null)
                    {
                        _loggerService.Info($"[PAYMENT_FOR_ORDER] Adding product to payment: {detail.Product.ProductName}, Quantity: {detail.Quantity}");
                        itemList.Add(new ItemData($"{detail.Product.ProductName} x{detail.Quantity}", detail.Quantity,
                            (int)detail.UnitPrice));
                    }
                    else
                    {
                        // If product is null, use a generic name
                        _loggerService.Warn($"[PAYMENT_FOR_ORDER] Product data missing for detail with ProductId: {detail.ProductId}, using fallback name");
                        itemList.Add(new ItemData($"Product ID: {detail.ProductId} x{detail.Quantity}", detail.Quantity,
                            (int)detail.UnitPrice));
                    }
                }
            }
            else
            {
                // If no order details, create a single generic item
                _loggerService.Warn($"[PAYMENT_FOR_ORDER] No order details available, creating generic payment item");
                itemList.Add(new ItemData("Order Payment", 1, (int)order.TotalAmount));
            }
            _loggerService.Info($"[PAYMENT_FOR_ORDER] Phase 3: Item list prepared - {itemList.Count} items");

            var orderCode = DateTime.Now.Ticks % 1000000000; // Simple order code generation

            // PayOS requires description max 25 chars, so use first 5 chars of order GUID
            var shortOrderId = order.Id.ToString().Substring(0, 5);
            var paymentDescription = $"Uniseap payment #{shortOrderId}";

            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 3: PayOS data configured - OrderCode: {orderCode}, Description: {paymentDescription}");

            var paymentData = new PaymentData(
                orderCode,
                (int)order.TotalAmount,
                paymentDescription,
                itemList,
                defaultRedirectUrl,
                cancelUrl
            );

            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 3: PaymentData created with Return URLs - Success: {defaultRedirectUrl}, Cancel: {cancelUrl}");
            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 3: Webhook URL for server notifications: {webhookUrl} (configured in PayOS merchant settings)");

            _loggerService.Info("[PAYMENT_FOR_ORDER] Phase 4: Calling PayOS createPaymentLink API");
            var paymentResult = await _payOs.createPaymentLink(paymentData);
            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 4 SUCCESS: PayOS API response received - CheckoutUrl: {paymentResult.checkoutUrl}");

            _loggerService.Info("[PAYMENT_FOR_ORDER] Phase 5: Updating payment record with PayOS response");
            payment.GatewayTransactionId = paymentResult.orderCode.ToString();
            payment.PaymentUrl = paymentResult.checkoutUrl;
            payment.GatewayResponse = JsonConvert.SerializeObject(paymentResult);

            await _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] Phase 5 SUCCESS: Payment record updated with GatewayTransactionId: {payment.GatewayTransactionId}");

            _loggerService.Info(
                $"[PAYMENT_FOR_ORDER] PROCESS COMPLETED SUCCESSFULLY - OrderId: {order.Id}, PaymentId: {payment.Id}, CheckoutUrl: {paymentResult.checkoutUrl}");

            return paymentResult.checkoutUrl;
        }
        catch (Exception ex)
        {
            _loggerService.Error(
                $"[PAYMENT_FOR_ORDER] PROCESS FAILED - OrderId: {orderId}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            throw ErrorHelper.BadRequest("Unable to create payment link. Please try again later.");
        }
    }

    public async Task ProcessWebhook(WebhookType webhookData)
    {
        _loggerService.Info(
            $"[WEBHOOK] Starting webhook processing - Data: {JsonConvert.SerializeObject(webhookData)}");
        try
        {
            _loggerService.Info("[WEBHOOK] Phase 1: Verifying webhook data with PayOS");
            var data = _payOs.verifyPaymentWebhookData(webhookData);
            _loggerService.Info(
                $"[WEBHOOK] Phase 1 SUCCESS: Webhook verified - OrderCode: {data.orderCode}, Status: {data.code}");

            _loggerService.Info($"[WEBHOOK] Phase 2: Finding payment record for OrderCode: {data.orderCode}");
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p =>
                p.GatewayTransactionId == data.orderCode.ToString());

            if (payment == null)
            {
                _loggerService.Error($"[WEBHOOK] Phase 2 FAILED: Payment not found for orderCode: {data.orderCode}");
                return;
            }

            _loggerService.Info(
                $"[WEBHOOK] Phase 2 SUCCESS: Payment found - PaymentId: {payment.Id}, OrderId: {payment.OrderId}");

            _loggerService.Info("[WEBHOOK] Phase 3: Updating payment status to Completed");
            payment.Status = PaymentStatus.Completed;
            payment.GatewayResponse = JsonConvert.SerializeObject(data);
            await _unitOfWork.Payments.Update(payment);
            _loggerService.Info("[WEBHOOK] Phase 3 SUCCESS: Payment status updated to Completed");

            _loggerService.Info(
                $"[WEBHOOK] Phase 4: Updating order status to Completed for OrderId: {payment.OrderId}");
            await UpdateOrderStatus(payment.OrderId, OrderStatus.Completed);
            _loggerService.Info("[WEBHOOK] Phase 4 SUCCESS: Order status updated to Completed");

            _loggerService.Info(
                $"[WEBHOOK] PROCESSING COMPLETED SUCCESSFULLY - PaymentId: {payment.Id}, OrderId: {payment.OrderId}");
        }
        catch (Exception ex)
        {
            _loggerService.Error($"[WEBHOOK] PROCESSING FAILED - Error: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    public async Task ProcessWebhookGet(long orderCode, string status, string code)
    {
        _loggerService.Info(
            $"[WEBHOOK-GET] Starting GET webhook processing - OrderCode: {orderCode}, Status: {status}, Code: {code}");
        try
        {
            _loggerService.Info($"[WEBHOOK-GET] Phase 1: Finding payment record for OrderCode: {orderCode}");
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p =>
                p.GatewayTransactionId == orderCode.ToString());

            if (payment == null)
            {
                _loggerService.Error($"[WEBHOOK-GET] Phase 1 FAILED: Payment not found for orderCode: {orderCode}");
                return;
            }

            _loggerService.Info(
                $"[WEBHOOK-GET] Phase 1 SUCCESS: Payment found - PaymentId: {payment.Id}, OrderId: {payment.OrderId}");

            // Check if status indicates successful payment
            if (status == "PAID" && code == "00")
            {
                _loggerService.Info("[WEBHOOK-GET] Phase 2: Payment successful, updating status to Completed");
                payment.Status = PaymentStatus.Completed;
                payment.GatewayResponse = $"GET Webhook: code={code}, status={status}, orderCode={orderCode}";
                await _unitOfWork.Payments.Update(payment);
                _loggerService.Info("[WEBHOOK-GET] Phase 2 SUCCESS: Payment status updated to Completed");

                _loggerService.Info(
                    $"[WEBHOOK-GET] Phase 3: Updating order status to Completed for OrderId: {payment.OrderId}");
                await UpdateOrderStatus(payment.OrderId, OrderStatus.Completed);
                _loggerService.Info("[WEBHOOK-GET] Phase 3 SUCCESS: Order status updated to Completed");
            }
            else
            {
                _loggerService.Info($"[WEBHOOK-GET] Payment not successful - Status: {status}, Code: {code}");
                if (status == "CANCELLED" || code != "00")
                {
                    payment.Status = PaymentStatus.Cancelled;
                    await _unitOfWork.Payments.Update(payment);
                    await UpdateOrderStatus(payment.OrderId, OrderStatus.Cancelled);
                    _loggerService.Info("[WEBHOOK-GET] Payment cancelled");
                }
            }

            _loggerService.Info(
                $"[WEBHOOK-GET] PROCESSING COMPLETED SUCCESSFULLY - PaymentId: {payment.Id}, OrderId: {payment.OrderId}");
        }
        catch (Exception ex)
        {
            _loggerService.Error(
                $"[WEBHOOK-GET] PROCESSING FAILED - OrderCode: {orderCode}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    public async Task<PaymentStatusDto> GetPaymentStatus(Guid paymentId)
    {
        _loggerService.Info($"[GET_STATUS] Getting payment status for PaymentId: {paymentId}");
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);

        if (payment == null)
        {
            _loggerService.Error($"[GET_STATUS] Payment not found for PaymentId: {paymentId}");
            throw ErrorHelper.NotFound("Payment not found");
        }

        _loggerService.Info($"[GET_STATUS] Payment found - Status: {payment.Status}, OrderId: {payment.OrderId}");

        // Auto-sync with PayOS if payment is still pending
        if (payment.Status == PaymentStatus.Pending)
        {
            _loggerService.Info(
                $"[GET_STATUS] Payment is pending, syncing with PayOS - GatewayTransactionId: {payment.GatewayTransactionId}");
            try
            {
                var paymentInfo = await _payOs.getPaymentLinkInformation(long.Parse(payment.GatewayTransactionId!));
                _loggerService.Info($"[GET_STATUS] PayOS sync response - Status: {paymentInfo.status}");

                if (paymentInfo.status == "PAID" && payment.Status != PaymentStatus.Completed)
                {
                    _loggerService.Info("[GET_STATUS] Updating payment status to Completed");
                    payment.Status = PaymentStatus.Completed;
                    await _unitOfWork.Payments.Update(payment);

                    await UpdateOrderStatus(payment.OrderId, OrderStatus.Completed);
                    await _unitOfWork.SaveChangesAsync();
                    _loggerService.Info("[GET_STATUS] Payment and order status updated to Completed");
                }
                else if (paymentInfo.status == "CANCELLED" && payment.Status != PaymentStatus.Cancelled)
                {
                    _loggerService.Info("[GET_STATUS] Updating payment status to Cancelled");
                    payment.Status = PaymentStatus.Cancelled;
                    await _unitOfWork.Payments.Update(payment);

                    await UpdateOrderStatus(payment.OrderId, OrderStatus.Cancelled);
                    await _unitOfWork.SaveChangesAsync();
                    _loggerService.Info("[GET_STATUS] Payment and order status updated to Cancelled");
                }
                else
                {
                    _loggerService.Info(
                        $"[GET_STATUS] No status change needed - PayOS status: {paymentInfo.status}, Current status: {payment.Status}");
                }
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[GET_STATUS] Error syncing with PayOS - Error: {ex.Message}");
            }
        }
        else
        {
            _loggerService.Info($"[GET_STATUS] Payment not pending, no sync needed - Current status: {payment.Status}");
        }

        return new PaymentStatusDto
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status.ToString(),
            PaymentUrl = payment.PaymentUrl,
            Amount = payment.Amount,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    public async Task<PaymentStatusDto> GetPaymentStatusOnly(Guid paymentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);

        if (payment == null)
            throw ErrorHelper.NotFound("Payment not found");

        // Read-only method - no sync with PayOS
        return new PaymentStatusDto
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status.ToString(),
            PaymentUrl = payment.PaymentUrl,
            Amount = payment.Amount,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    public async Task<PaymentStatusDto> GetPaymentByOrderCode(string orderCode)
    {
        _loggerService.Info($"[GET_BY_ORDERCODE] Getting payment by OrderCode: {orderCode}");
        var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(p =>
            p.GatewayTransactionId == orderCode);

        if (payment == null)
        {
            _loggerService.Error($"[GET_BY_ORDERCODE] Payment not found for OrderCode: {orderCode}");
            throw ErrorHelper.NotFound("Payment not found");
        }

        _loggerService.Info(
            $"[GET_BY_ORDERCODE] Payment found - PaymentId: {payment.Id}, OrderId: {payment.OrderId}, Status: {payment.Status}");

        // Read-only method - no sync with PayOS
        return new PaymentStatusDto
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status.ToString(),
            PaymentUrl = payment.PaymentUrl,
            Amount = payment.Amount,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    public async Task<bool> CancelPayment(Guid paymentId, string reason)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
            throw ErrorHelper.NotFound("Payment not found");

        if (payment.Status != PaymentStatus.Pending)
            throw ErrorHelper.BadRequest("Only pending payments can be cancelled");

        try
        {
            var result = await _payOs.cancelPaymentLink(long.Parse(payment.GatewayTransactionId!), reason);

            payment.Status = PaymentStatus.Cancelled;
            payment.GatewayResponse = JsonConvert.SerializeObject(result);
            await _unitOfWork.Payments.Update(payment);

            await UpdateOrderStatus(payment.OrderId, OrderStatus.Cancelled);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"Payment {paymentId} cancelled successfully");
            return true;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Failed to cancel payment: {ex.Message}");
            throw ErrorHelper.BadRequest("Cannot cancel payment");
        }
    }

    public async Task<List<PaymentStatusDto>> GetPaymentsByOrderId(Guid orderId)
    {
        var payments = await _unitOfWork.Payments.GetAllAsync(p => p.OrderId == orderId);

        return payments.Select(p => new PaymentStatusDto
        {
            PaymentId = p.Id,
            OrderId = p.OrderId,
            Status = p.Status.ToString(),
            PaymentUrl = p.PaymentUrl,
            Amount = p.Amount,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }

    public async Task<OrderDto> CreateOrderFromCart(Guid customerId, CreateOrderDto createOrderDto)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            if (customer == null)
                throw ErrorHelper.NotFound("Customer not found");

            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId);
            if (cart == null)
                throw ErrorHelper.NotFound("Cart not found");

            var cartItems = await _unitOfWork.CartItems.GetAllAsync(ci => ci.CartId == cart.Id);
            if (!cartItems.Any())
                throw ErrorHelper.BadRequest("No items in cart");

            // Check if any items are already selected (checked)
            var checkedItems = cartItems.Where(ci => ci.IsCheck).ToList();

            if (!checkedItems.Any())
            {
                // If no items are checked, auto-check all items for payment
                foreach (var item in cartItems)
                {
                    item.IsCheck = true;
                    await _unitOfWork.CartItems.Update(item);
                }

                await _unitOfWork.SaveChangesAsync();
                checkedItems = cartItems.ToList();

                _loggerService.Info($"Auto-checked all {cartItems.Count} items for payment");
            }
            else
            {
                _loggerService.Info($"Using {checkedItems.Count} pre-selected items for payment");
            }

            cartItems = checkedItems;

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                ShipAddress = createOrderDto.ShipAddress,
                PaymentMethod = createOrderDto.PaymentGateway.ToString(),
                Status = OrderStatus.Pending,
                Customer = customer
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var orderDetails = new List<OrderDetail>();
            foreach (var cartItem in cartItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
                if (product == null) continue;

                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * cartItem.Quantity,
                    Order = order,
                    Product = product
                };

                orderDetails.Add(orderDetail);
                await _unitOfWork.OrdersDetail.AddAsync(orderDetail);

                // Update product quantity
                product.Quantity -= cartItem.Quantity;
                await _unitOfWork.Products.Update(product);

                // Remove from cart
                await _unitOfWork.CartItems.SoftRemove(cartItem);
            }

            await _unitOfWork.SaveChangesAsync();

            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                ShipAddress = order.ShipAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                TotalAmount = (decimal)orderDetails.Sum(od => od.TotalPrice),
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

    public async Task<OrderDto> UpdateOrderStatus(Guid orderId, OrderStatus status)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            if (order == null)
                throw ErrorHelper.NotFound($"Order with ID: {orderId} not found");

            // Load order details separately
            var orderDetails = await _unitOfWork.OrdersDetail.GetAllAsync(od => od.OrderId == orderId);

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (status == OrderStatus.Completed)
                order.CompletedDate = DateTime.UtcNow;

            await _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"Updated order {orderId} status to {status}");

            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                ShipAddress = order.ShipAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                CompletedDate = order.CompletedDate,
                CancellationReason = order.CancellationReason,
                TotalAmount = (decimal)orderDetails.Sum(od => od.TotalPrice),
                OrderDetails = orderDetails.Select(od => new OrderDetailDto
                {
                    Id = od.Id,
                    ProductId = od.ProductId,
                    ProductName = od.Product?.ProductName ?? "Unknown Product",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    TotalPrice = od.TotalPrice
                }).ToList()
            };
        }
        catch (Exception e)
        {
            _loggerService.Error($"Error updating order {orderId} status: {e.Message}");
            throw;
        }
    }
}