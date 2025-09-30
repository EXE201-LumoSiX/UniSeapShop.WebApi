# UniSeapShop Web API

Nền tảng mua bán đồ second-hand. Người dùng có thể đăng ký/đăng nhập, duyệt sản phẩm, thêm vào giỏ, thanh toán và theo dõi đơn hàng. Người bán có thể xem doanh thu ở mức cơ bản. Quản trị viên (admin) quản lý người dùng, sản phẩm và đơn hàng.

## Vai trò hệ thống
- Người mua (User)
- Người bán (Supplier)
- Quản trị viên (Admin)

## Tính năng

- Đăng ký/đăng nhập người dùng (JWT)
- Danh sách sản phẩm cùng thông tin tình trạng (condition)
- Giỏ hàng (Shopping cart)
- Thanh toán (Checkout) – luồng cơ bản
- Quản lý đơn hàng cơ bản (Basic order management)

- Hệ thống đánh giá/feedback
- Mã giảm giá/Voucher & khuyến mãi (Promotions)

## Kiến trúc & Công nghệ
- ASP.NET Core 8 Web API, Swagger UI tích hợp
- Entity Framework Core + SQL Server
- Xác thực JWT
- Dockerfile + docker-compose cho phát triển nhanh

Cấu trúc solution (rút gọn):
- `UniSeapShop.API/` – Web API (Controllers, Program, Dockerfile)
- `UniSeapShop.Application/` – Application layer (Services, Interfaces)
- `UniSeapShop.Domain/` – Domain layer (Entities, DTOs, DbContext, Migrations)
- `UniSeapShop.Infrastructure/` – Infrastructure (Repositories, UoW, tích hợp ngoài)

Swagger: được bật ở Development/Production, truy cập ngay trang chủ.

## Yêu cầu môi trường
- .NET SDK 8.0+
- SQL Server (local hoặc trong Docker)
- PowerShell (Windows) hoặc Bash (Linux/macOS)

## Chạy bằng Docker Compose (dành cho FE dev)
- API expose trên cổng 5000 (ứng dụng cấu hình `UseUrls("http://0.0.0.0:5000")`).
- SQL Server sẽ chạy trong container kèm theo.

PowerShell (Windows):

```powershell
# Build & chạy nền
docker compose up -d --build

# Xem log API
docker compose logs -f uniseapshop.webapi

# Dừng
docker compose down
```

Sau khi lên, mở trình duyệt:
- Swagger UI: http://localhost:5000

EF Core migrations sẽ được tự áp dụng khi khởi động (Program.cs có `app.ApplyMigrations(...)`).

### Thông tin từ docker-compose
- Service API: `uniseapshop.webapi`
	- Port: Host `5000` -> Container `5000`
	- Swagger: http://localhost:5000
- Service SQL Server: `uniseapshop.database`
	- Image: `mcr.microsoft.com/mssql/server:2022-latest`
	- Port: Host `1434` -> Container `1433`
	- Tài khoản: `sa` / Mật khẩu: `UniSeap@123`
	- Database mặc định: `UniSeapShopDB` (được tạo/migrate khi API khởi động lần đầu)

## Kết nối SQL Server bằng SSMS
Bạn có thể đăng nhập vào SQL Server container để xem dữ liệu:

1) Mở Microsoft SQL Server Management Studio (SSMS)
2) Tại cửa sổ Connect to Server:
	 - Server type: Database Engine
	 - Server name: `localhost,1434`
	 - Authentication: SQL Server Authentication
	 - Login: `sa`
	 - Password: `UniSeap@123`
3) Nhấn Connect. Sau đó chọn database `UniSeapShopDB` trong Object Explorer.

Mẹo xử lý sự cố:
- Nếu kết nối thất bại, kiểm tra container đang chạy: `docker compose ps`
- Nếu báo lỗi về mã hóa/chứng chỉ, trong SSMS -> Options -> tab Connection Properties:
	- Đặt Encrypt: Optional (hoặc tích Trust server certificate)
- Kiểm tra cổng máy chủ có đúng với `docker-compose.yml` (mặc định 1434)

## API mẫu (Auth)
- Đăng nhập: `POST /api/Authe/login`
- Đăng ký: `POST /api/Authe/register`
