# Hướng dẫn Luồng Thanh toán UniSeapShop

> Hướng dẫn ngắn gọn giúp bạn hiểu và tích hợp với luồng thanh toán của UniSeapShop qua PayOS.

## Luồng Thanh toán

1. **Thêm vào giỏ hàng**: Người dùng thêm sản phẩm vào giỏ hàng.
2. **Tạo đơn hàng**: Người dùng nhập địa chỉ giao hàng và tạo đơn hàng từ giỏ hàng bằng `POST /api/orders`.
3. **Tạo link thanh toán**: Sau khi có đơn hàng, tạo link thanh toán bằng `POST /api/payments/create-link/{orderId}`.
4. **Chuyển hướng đến PayOS**: Người dùng được chuyển đến cổng thanh toán PayOS.
5. **Webhook (Thông báo tự động)**: Sau khi thanh toán, PayOS gửi một thông báo đến `POST /api/payments/webhook`.
6. **Cập nhật trạng thái**: Hệ thống xác thực webhook và cập nhật trạng thái đơn hàng và thanh toán.
7. **Chuyển hướng về Trang Thành công**: Người dùng được chuyển về trang thông báo thanh toán thành công của cửa hàng.

---

## Các API Endpoints

| Phương thức | Endpoint                              | Mô tả                                                  |
|:------------|:--------------------------------------|:-------------------------------------------------------|
| `POST`      | `/api/cart/items`                     | Thêm một sản phẩm vào giỏ hàng.                        |
| `GET`       | `/api/cart`                           | Xem giỏ hàng hiện tại.                                 |
| `PATCH`     | `/api/cart/items/check`               | Chọn/bỏ chọn một sản phẩm cụ thể trong giỏ.            |
| `PATCH`     | `/api/cart/items/check-all`           | Chọn/bỏ chọn tất cả sản phẩm trong giỏ.                |
| `POST`      | `/api/orders`                         | Tạo đơn hàng từ giỏ hàng.                              |
| `POST`      | `/api/payments/create-link/{orderId}` | Tạo link thanh toán cho đơn hàng có sẵn.               |
| `GET`       | `/api/payments/{paymentId}`           | Kiểm tra trạng thái của một thanh toán cụ thể.         |
| `POST`      | `/api/payments/webhook`               | **[Nội bộ]** Endpoint nhận thông báo tự động từ PayOS. |

---

## Ví dụ Request & Response

<details>
<summary><strong>POST /api/orders (Tạo đơn hàng)</strong></summary>

**Request Body (Dữ liệu gửi đi):**

```json
{
  "shipAddress": "123 Đường ABC, Quận 1, TP.HCM",
  "paymentGateway": 0
}
```

**Success Response (Phản hồi thành công):**

```json
{
  "isSuccess": true,
  "value": {
    "code": "200",
    "message": "Tạo đơn hàng thành công",
    "data": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "customerId": "550e8400-e29b-41d4-a716-446655440001",
      "orderDate": "2025-10-13T10:30:00Z",
      "shipAddress": "123 Đường ABC, Quận 1, TP.HCM",
      "paymentMethod": "PayOS",
      "status": "Pending",
      "totalAmount": 150000,
      "orderDetails": [
        {
          "id": "550e8400-e29b-41d4-a716-446655440002",
          "productId": "550e8400-e29b-41d4-a716-446655440003",
          "productName": "Áo thun nam",
          "productImage": "https://example.com/image.jpg",
          "quantity": 2,
          "unitPrice": 75000,
          "totalPrice": 150000
        }
      ]
    }
  }
}
```

</details>

<details>
<summary><strong>POST /api/payments/create-link/{orderId} (Tạo link thanh toán)</strong></summary>

**URL Path Parameter:**

- `orderId`: ID của đơn hàng đã tạo (ví dụ: 550e8400-e29b-41d4-a716-446655440000)

**Success Response (Phản hồi thành công):**

```json
{
  "isSuccess": true,
  "value": {
    "code": "200",
    "message": "Tạo link thanh toán thành công",
    "data": "https://pay.payos.vn/web/..."
  }
}
```

</details>

<details>
<summary><strong>GET /api/payments/{paymentId}</strong></summary>

**Success Response (Phản hồi thành công):**

```json
{
  "isSuccess": true,
  "value": {
    "data": {
      "paymentId": "đây-là-guid",
      "orderId": "đây-là-guid",
      "status": "Completed",
      "paymentUrl": "https://pay.payos.vn/web/...",
      "amount": 150000,
      "createdAt": "2024-10-07T10:30:00Z",
      "updatedAt": "2024-10-07T10:31:00Z"
    }
  }
}
```

</details>

---

## Các Trạng thái Thanh toán

| Trạng thái  | Mô tả                                        |
|:------------|:---------------------------------------------|
| `Pending`   | Đang chờ người dùng thanh toán.              |
| `Completed` | Thanh toán thành công, đơn hàng đã được tạo. |
| `Cancelled` | Người dùng đã hủy thanh toán.                |
| `Failed`    | Thanh toán thất bại do lỗi.                  |
| `Refunded`  | Thanh toán đã được hoàn tiền.                |

---

## Tích hợp Frontend

<details>
<summary><strong>Ví dụ: Tạo đơn hàng và thanh toán riêng biệt</strong></summary>

```javascript
// 1. Tạo đơn hàng từ giỏ hàng
async function createOrder() {
  try {
    const response = await fetch('/api/orders', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${your_jwt_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        shipAddress: '123 Đường ABC, Quận 1, TP.HCM',
        paymentGateway: 0
      })
    });

    const result = await response.json();

    if (result.isSuccess) {
      const orderId = result.value.data.id;
      console.log('Đơn hàng đã tạo:', orderId);
      
      // 2. Tạo link thanh toán cho đơn hàng
      return await createPaymentForOrder(orderId);
    } else {
      console.error('Tạo đơn hàng thất bại:', result.value.message);
    }
  } catch (error) {
    console.error('Đã xảy ra lỗi khi tạo đơn hàng:', error);
  }
}

// 2. Tạo link thanh toán cho đơn hàng có sẵn
async function createPaymentForOrder(orderId) {
  try {
    const response = await fetch(`/api/payments/create-link/${orderId}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${your_jwt_token}`
      }
    });

    const result = await response.json();

    if (result.isSuccess) {
      // 3. Chuyển hướng người dùng đến URL của PayOS
      window.location.href = result.value.data;
    } else {
      console.error('Tạo link thanh toán thất bại:', result.value.message);
    }
  } catch (error) {
    console.error('Đã xảy ra lỗi khi tạo link thanh toán:', error);
  }
}

// Sử dụng
createOrder();
```

</details>

---

## Lỗi Thường gặp & Cấu hình

| Lỗi                                  | Nguyên nhân có thể                                          |
|:-------------------------------------|:------------------------------------------------------------|
| `Customer not found`                 | Người dùng chưa đăng nhập (thiếu hoặc sai JWT token).       |
| `Cart not found`                     | Giỏ hàng của người dùng trống hoặc không tồn tại.           |
| `No items in cart`                   | Giỏ hàng có tồn tại nhưng không có sản phẩm nào.            |
| `Order not found`                    | ID đơn hàng không tồn tại hoặc không phải của người dùng.   |
| `Order is not available for payment` | Đơn hàng không ở trạng thái Pending (có thể đã thanh toán). |
| `Insufficient stock`                 | Không đủ hàng trong kho cho một hoặc nhiều sản phẩm.        |
| `PayOS error`                        | Sai API keys trong file cấu hình môi trường.                |

## Ưu điểm của Luồng Thanh toán

1. **Tách biệt rõ ràng**: Tạo đơn hàng và thanh toán là hai bước riêng biệt.
2. **Kiểm soát tốt hơn**: Người dùng có thể xem lại đơn hàng trước khi thanh toán.
3. **Xử lý lỗi dễ dàng**: Lỗi khi tạo đơn hàng và thanh toán được xử lý riêng biệt.
4. **Linh hoạt hơn**: Có thể tạo đơn hàng mà không cần thanh toán ngay lập tức.
5. **Test dễ dàng**: Mỗi bước có thể được test độc lập.
