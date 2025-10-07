# Hướng dẫn Luồng Thanh toán UniSeapShop

> Hướng dẫn ngắn gọn giúp bạn hiểu và tích hợp với luồng thanh toán của UniSeapShop qua PayOS.

## Cơ chế của PayOS

1.  **Thêm vào giỏ hàng**: Người dùng thêm sản phẩm vào giỏ hàng.
2.  **Thanh toán**: Người dùng bắt đầu thanh toán. Hệ thống có thể sử dụng các sản phẩm đã được chọn sẵn hoặc tự động chọn tất cả nếu chưa có sản phẩm nào được chọn.
3.  **Tạo Link Thanh toán**: Backend gọi đến `POST /api/payments/create-link`.
4.  **Chuyển hướng đến PayOS**: Người dùng được chuyển đến cổng thanh toán PayOS.
5.  **Webhook (Thông báo tự động)**: Sau khi thanh toán, PayOS gửi một thông báo đến `POST /api/payments/webhook`.
6.  **Tạo Đơn hàng**: Hệ thống xác thực webhook, tạo đơn hàng và cập nhật trạng thái thanh toán.
7.  **Chuyển hướng về Trang Thành công**: Người dùng được chuyển về trang thông báo thanh toán thành công của cửa hàng.

---

## Các API Endpoints

| Phương thức | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `POST` | `/api/cart/items` | Thêm một sản phẩm vào giỏ hàng. |
| `GET` | `/api/cart` | Xem giỏ hàng hiện tại. |
| `PATCH` | `/api/cart/items/check` | Chọn/bỏ chọn một sản phẩm cụ thể trong giỏ. |
| `PATCH` | `/api/cart/items/check-all` | Chọn/bỏ chọn tất cả sản phẩm trong giỏ. |
| `POST` | `/api/payments/create-link` | Tạo link thanh toán PayOS từ giỏ hàng. |
| `GET` | `/api/payments/{paymentId}` | Kiểm tra trạng thái của một thanh toán cụ thể. |
| `POST` | `/api/payments/webhook` | **[Nội bộ]** Endpoint nhận thông báo tự động từ PayOS. |

---

## Ví dụ Request & Response

<details>
<summary><strong>POST /api/payments/create-link</strong></summary>

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

| Trạng thái | Mô tả |
| :--- | :--- |
| `Pending` | Đang chờ người dùng thanh toán. |
| `Completed` | Thanh toán thành công, đơn hàng đã được tạo. |
| `Cancelled` | Người dùng đã hủy thanh toán. |
| `Failed` | Thanh toán thất bại do lỗi. |
| `Refunded` | Thanh toán đã được hoàn tiền. |

---

## Tích hợp Frontend

<details>
<summary><strong>Ví dụ: Tạo thanh toán và chuyển hướng</strong></summary>

```javascript
// 1. Gọi backend để tạo link thanh toán
async function createPayment() {
  try {
    const response = await fetch('/api/payments/create-link', {
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
      // 2. Chuyển hướng người dùng đến URL của PayOS
      window.location.href = result.value.data;
    } else {
      console.error('Tạo link thanh toán thất bại:', result.value.message);
    }
  } catch (error) {
    console.error('Đã xảy ra lỗi:', error);
  }
}
```
</details>

---

## Lỗi Thường gặp & Cấu hình

| Lỗi | Nguyên nhân có thể |
| :--- | :--- |
| `Customer not found` | Người dùng chưa đăng nhập (thiếu hoặc sai JWT token). |
| `Cart not found` | Giỏ hàng của người dùng trống hoặc không tồn tại. |
| `No items in cart` | Giỏ hàng có tồn tại nhưng không có sản phẩm nào. |
| `PayOS error` | Sai API keys trong file cấu hình môi trường. |

**Biến môi trường (`.env`):**
```env
PAYOS_CLIENT_ID=your_client_id
PAYOS_API_KEY=your_api_key
PAYOS_CHECKSUM_KEY=your_checksum_key
```