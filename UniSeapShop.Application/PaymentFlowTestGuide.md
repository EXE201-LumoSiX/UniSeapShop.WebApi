// Payment Flow Test Guide
// Hướng dẫn test luồng thanh toán PayOS trong UniSeapShop

/*
1. Environment Variables cần thiết:
   - PAYOS_CLIENT_ID=your_client_id
   - PAYOS_API_KEY=your_api_key  
   - PAYOS_CHECKSUM_KEY=your_checksum_key

1.1. Default Redirect URLs (được system tự động sử dụng):
   - Success URL: https://uniseapshop.vercel.app/payment-success
   - Cancel URL: https://uniseapshop.vercel.app/payment-cancel

2. Test Flow:
   
   a) Thêm sản phẩm vào giỏ hàng:
   POST /api/cart/items
   {
     "productId": "guid",
     "quantity": 1
   }

   a.1) Check/uncheck sản phẩm cụ thể (Optional):
   PATCH /api/cart/items/check
   {
     "productId": "guid",
     "isCheck": true
   }

   a.2) Check/uncheck tất cả sản phẩm (Optional):
   PATCH /api/cart/items/check-all
   {
     "isCheckAll": true
   }

   b) Tạo link thanh toán:
   POST /api/payments/create-link
   {
     "shipAddress": "123 Test Street", 
     "paymentGateway": 0, // PayOS
     "selectedProductIds": ["guid1", "guid2"] // Optional: specific products
   }
   
   Response: Payment URL từ PayOS (system tự động check tất cả items hoặc items được chọn)

   c) Sau khi thanh toán thành công, PayOS sẽ gọi webhook:
   POST /api/payments/webhook
   (PayOS tự động gọi)

   d) Kiểm tra trạng thái thanh toán:
   GET /api/payments/{paymentId}

   e) Xem lịch sử thanh toán của đơn hàng:
   GET /api/payments/order/{orderId}

3. Test Cases:
   - Thanh toán thành công
   - Hủy thanh toán
   - Webhook xử lý
   - Đồng bộ trạng thái với PayOS
   - Check/uncheck items trước thanh toán
   - Thanh toán với items đã được chọn
   - Auto-check items nếu chưa có item nào được chọn

4. Database Changes:
   Payment entity đã được cập nhật với các fields:
   - Amount (decimal)
   - PaymentGateway (enum)
   - Status (enum)  
   - GatewayTransactionId (string?)
   - PaymentUrl (string?)
   - RedirectUrl (string?)
   - GatewayResponse (string?)

5. Order Flow:
   Option 1: Manual Check
   Cart Items → User checks items → Payment → Order → OrderDetails → PayOS
   
   Option 2: Auto Check  
   Cart Items (unchecked) → Payment (auto-check all) → Order → OrderDetails → PayOS

6. New Cart APIs:
   - PATCH /api/cart/items/check - Check/uncheck single item
   - PATCH /api/cart/items/check-all - Check/uncheck all items
   - GET /api/cart - View current cart with check status
*/