# Payment Flow Guide

## Luồng thanh toán
1. Thêm sản phẩm vào giỏ → Chọn items → Thanh toán → PayOS → Tạo đơn hàng

## Tổng quan luồng
UniSeapShop sử dụng PayOS làm cổng thanh toán chính. Luồng thanh toán được thiết kế đơn giản và an toàn cho người dùng.

## APIs chính

### Luồng chính (dành cho người non-tech)
1. Khách hàng thêm sản phẩm vào giỏ hàng
2. Chọn sản phẩm muốn mua (hoặc hệ thống tự chọn tất cả)
3. Nhấn "Thanh toán" → Hệ thống tạo link PayOS
4. Khách hàng được chuyển đến trang PayOS để thanh toán
5. PayOS gọi webhook về hệ thống
6. Hệ thống cập nhật trạng thái và tạo đơn hàng
7. Khách hàng được chuyển về trang thành công

### Giỏ hàng
POST /api/cart/items                    # Thêm sản phẩm
GET /api/cart                          # Xem giỏ hàng
PATCH /api/cart/items/check            # Chọn 1 sản phẩm
PATCH /api/cart/items/check-all        # Chọn tất cả

### Thanh toán
POST /api/payments/create-link         # Tạo link PayOS
GET /api/payments/{paymentId}          # Kiểm tra trạng thái
POST /api/payments/webhook             # PayOS webhook

## Request/Response mẫu

### Tạo thanh toán
POST /api/payments/create-link
Request body:
{
  "shipAddress": "Địa chỉ giao hàng",
  "paymentGateway": 0
}

Response (ví dụ):
{
  "isSuccess": true,
  "value": {
    "data": "https://pay.payos.vn/web/..."
  }
}

### Kiểm tra trạng thái
GET /api/payments/{paymentId}
Trạng thái chính: Pending, Completed, Cancelled, Failed, Refunded

## Frontend flow (ngắn gọn)
// 1. Tạo link thanh toán
const response = await fetch('/api/payments/create-link', {
  method: 'POST',
  headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
  body: JSON.stringify({ shipAddress: 'địa chỉ', paymentGateway: 0 })
});
const result = await response.json();
const paymentUrl = result.value.data;

// 2. Redirect
window.location.href = paymentUrl;

// 3. Khi quay về, check status
const status = await fetch(`/api/payments/${paymentId}`, { headers: { 'Authorization': 'Bearer ' + token } });

## Cấu hình (env)
PAYOS_CLIENT_ID=your_client_id
PAYOS_API_KEY=your_api_key
PAYOS_CHECKSUM_KEY=your_checksum_key

## Lỗi thường gặp
- "Customer not found" → Chưa login
- "Cart not found" → Giỏ hàng trống
- "No items in cart" → Chưa có sản phẩm
- PayOS error → Kiểm tra API keys