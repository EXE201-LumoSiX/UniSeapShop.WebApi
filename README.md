## Quy trình làm việc với Git

> **QUAN TRỌNG:**  
> 1. **LUÔN CẬP NHẬT CODE TỪ MASTER TRƯỚC KHI TẠO NHÁNH MỚI** để tránh conflict!  
> 2. **KHÔNG PUSH TRỰC TIẾP LÊN MASTER!**

Quy trình làm việc:
1. Update code từ master: `git checkout master && git pull`
2. Tạo nhánh mới: `git checkout -b feature/ten-tinh-nang`
3. Commit và push: `git push origin feature/ten-tinh-nang`
4. Tạo Pull Request để review trước khi merge

# UniSeapShop Web API

> **Lưu ý:** Trước khi test các chức năng CRUD, bạn cần gọi API seed dữ liệu tại SystemController (`/api/system/seed-all-data`) để khởi tạo dữ liệu mẫu cho hệ thống. Nếu không có dữ liệu, các chức năng sẽ không hoạt động đúng.

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


## Chạy bằng Docker Compose
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
- Service Redis: `redis`
	- Port: Host `6379` -> Container `6379`

## Kết nối SQL Server bằng SSMS
Bạn có thể đăng nhập vào SQL Server để xem dữ liệu:

1) Mở Microsoft SQL Server Management Studio (SSMS)
2) Tại cửa sổ Connect to Server:
	 - Server type: Database Engine
	 - Server name: `103.211.201.141,1433`
	 - Authentication: SQL Server Authentication
	 - Login: `sa`
	 - Password: `YourStrong!Passw0rd`
	 - Database: `UniSeapShopDB`
3) Nhấn Connect. Sau đó chọn database `UniSeapShopDB` trong Object Explorer.

Mẹo xử lý sự cố:
- Nếu kết nối thất bại, kiểm tra kết nối internet của bạn
- Nếu báo lỗi về mã hóa/chứng chỉ, trong SSMS -> Options -> tab Connection Properties:
	- Đặt Encrypt: Optional (hoặc tích Trust server certificate)
- Kiểm tra cấu hình trong docker-compose.yml cho thông tin kết nối mới nhất



## API mẫu (Auth)
- Đăng nhập: `POST /api/Authe/login`
- Đăng ký: `POST /api/Authe/register`
