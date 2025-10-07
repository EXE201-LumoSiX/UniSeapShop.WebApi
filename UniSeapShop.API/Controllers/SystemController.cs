using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Domain.Enums;

namespace UniSeapShop.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly UniSeapShopDBContext _context;
    private readonly ILoggerService _logger;


    public SystemController(UniSeapShopDBContext context, ILoggerService logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    ///     Xóa toàn bộ dữ liệu trong database và seed lại dữ liệu mẫu (roles, users, ...).
    ///     Sử dụng khi cần làm sạch và khởi tạo lại dữ liệu hệ thống.
    /// </summary>
    /// <returns>Thông báo thành công hoặc lỗi khi seed dữ liệu.</returns>
    [HttpPost("seed-all-data")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await ClearDatabase(_context);

            // Seed data
            await SeedRolesAndUsers();
            await SeedProductsAndCategories();
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Data seeded successfully."
            }));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.Error($"Database update error: {dbEx.Message}");
            return StatusCode(500, "Error seeding data: Database issue.");
        }
        catch (Exception ex)
        {
            _logger.Error($"General error: {ex.Message}");
            return StatusCode(500, "Error seeding data: General failure.");
        }
    }


    private async Task ClearDatabase(UniSeapShopDBContext context)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                _logger.Info("Bắt đầu xóa dữ liệu trong database...");

                var tablesToDelete = new List<Func<Task>>
                {
                    () => context.Feedbacks.ExecuteDeleteAsync(),
                    () => context.Vouchers.ExecuteDeleteAsync(),
                    () => context.PayoutDetails.ExecuteDeleteAsync(),
                    () => context.CartItems.ExecuteDeleteAsync(),
                    () => context.OrderDetails.ExecuteDeleteAsync(),
                    () => context.Orders.ExecuteDeleteAsync(),
                    () => context.Payments.ExecuteDeleteAsync(),
                    () => context.Products.ExecuteDeleteAsync(),
                    () => context.ProductImages.ExecuteDeleteAsync(),
                    () => context.Categories.ExecuteDeleteAsync(),
                    () => context.Carts.ExecuteDeleteAsync(),
                    () => context.Customers.ExecuteDeleteAsync(),
                    () => context.Suppliers.ExecuteDeleteAsync(),
                    () => context.Users.ExecuteDeleteAsync(),
                    () => context.Roles.ExecuteDeleteAsync()
                };

                foreach (var deleteFunc in tablesToDelete)
                    await deleteFunc();

                await transaction.CommitAsync();

                _logger.Success("Xóa sạch dữ liệu trong database thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.Error($"Xóa dữ liệu thất bại: {ex.Message}");
                throw;
            }
        });
    }

    /// <summary>
    ///     Seed các role và user mẫu vào hệ thống.
    ///     Trả về danh sách các account đã được seed (gồm tên, email, số điện thoại, role, password hash).
    /// </summary>
    /// <returns>Danh sách các account đã seed hoặc lỗi nếu có.</returns>
    [HttpPost("seed-roles-users")]
    public async Task<IActionResult> SeedRolesAndUsersEndpoint()
    {
        try
        {
            var seededUsers = await SeedRolesAndUsers();
            return Ok(ApiResult<List<object>>.Success(seededUsers, "200", "Seeded roles and users successfully"));
        }
        catch (Exception ex)
        {
            _logger.Error($"SeedRolesAndUsersEndpoint error: {ex.Message}");
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<object>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Seed các sản phẩm và danh mục mẫu vào hệ thống.
    ///     Trả về danh sách các sản phẩm đã được seed (gồm tên, danh mục, nhà cung cấp, giá, số lượng ảnh).
    /// </summary>
    [HttpPost("seed-products-categories")]
    public async Task<IActionResult> SeedProductsAndCategoriesEndpoint()
    {
        try
        {
            var seededProducts = await SeedProductsAndCategories();
            return Ok(ApiResult<List<object>>.Success(seededProducts, "200",
                "Seeded products and categories successfully"));
        }
        catch (Exception ex)
        {
            _logger.Error($"SeedProductsAndCategoriesEndpoint error: {ex.Message}");
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<object>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    private async Task<List<object>> SeedRolesAndUsers()
    {
        // 1️⃣ Seed các Role
        var roles = new List<Role>
        {
            new()
            {
                Name = "Customer",
                RoleType = RoleType.User,
                Description = "Người mua hàng, sử dụng các dịch vụ của hệ thống.",
                IsActive = true
            },
            new()
            {
                Name = "Supplier",
                RoleType = RoleType.User,
                Description = "Người bán hàng, đăng bán sản phẩm second-hand.",
                IsActive = true
            },
            new()
            {
                Name = "Admin",
                RoleType = RoleType.Admin,
                Description = "Quản trị viên hệ thống, toàn quyền quản lý.",
                IsActive = true
            }
        };

        foreach (var role in roles)
            if (!await _context.Roles.AnyAsync(r => r.RoleType == role.RoleType))
                await _context.Roles.AddAsync(role);

        await _context.SaveChangesAsync();

        // 2️⃣ Seed các User mẫu
        var users = new List<(User user, string plainPassword)>
        {
            (
                new User
                {
                    FullName = "Admin User",
                    Email = "admin@uniseapshop.com",
                    Password = new PasswordHasher().HashPassword("Admin123!"),
                    PhoneNumber = "0123456789",
                    RoleId = roles.First(r => r.RoleType == RoleType.Admin).Id,
                    Role = roles.First(r => r.RoleType == RoleType.Admin),
                    IsEmailVerify = true,
                    IsActive = true
                },
                "Admin123!"
            ),
            (
                new User
                {
                    FullName = "Supplier User",
                    Email = "supplier@uniseapshop.com",
                    Password = new PasswordHasher().HashPassword("Supplier123!"),
                    PhoneNumber = "0987654321",
                    RoleId = roles.First(r => r.Name == "Supplier").Id,
                    Role = roles.First(r => r.Name == "Supplier"),
                    IsEmailVerify = true,
                    IsActive = true
                },
                "Supplier123!"
            ),
            (
                new User
                {
                    FullName = "Customer User",
                    Email = "customer@uniseapshop.com",
                    Password = new PasswordHasher().HashPassword("Customer123!"),
                    PhoneNumber = "0111222333",
                    RoleId = roles.First(r => r.Name == "Customer").Id,
                    Role = roles.First(r => r.Name == "Customer"),
                    IsEmailVerify = true,
                    IsActive = true
                },
                "Customer123!"
            )
        };

        var seededUserList = new List<object>();

        foreach (var (user, plainPassword) in users)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser == null)
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync(); // cần save để có Id mới

                // 3️⃣ Nếu là Supplier thì seed thêm record trong bảng Supplier
                if (user.Role.Name == "Supplier")
                {
                    var supplier = new Supplier
                    {
                        UserId = user.Id,
                        User = user,
                        Description = "Nhà cung cấp chuyên bán sản phẩm chất lượng cao.",
                        Rating = 5.0f,
                        IsActive = true
                    };
                    await _context.Suppliers.AddAsync(supplier);
                    await _context.SaveChangesAsync();
                }

                // 4️⃣ Nếu là Customer thì seed thêm record trong bảng Customer (KHÔNG tạo Cart)
                if (user.Role.Name == "Customer")
                {
                    var customer = new Customer
                    {
                        UserId = user.Id,
                        User = user,
                        LoyaltyPoint = 0,
                        MembershipLevel = "Basic"
                    };
                    await _context.Customers.AddAsync(customer);
                    await _context.SaveChangesAsync();
                }

                existingUser = user;
            }

            seededUserList.Add(new
            {
                existingUser.FullName,
                existingUser.Email,
                existingUser.PhoneNumber,
                Role = existingUser.Role.Name,
                Password = plainPassword
            });
        }

        return seededUserList;
    }


    private async Task<List<object>> SeedProductsAndCategories()
    {
        // 1️⃣ Seed Category
        var categories = new List<Category>
        {
            new() { CategoryName = "Thời trang" },
            new() { CategoryName = "Điện tử" },
            new() { CategoryName = "Đồ gia dụng" },
            new() { CategoryName = "Sách & Văn phòng phẩm" },
            new() { CategoryName = "Đồ thể thao" },
            new() { CategoryName = "Trang sức & Phụ kiện" }
        };

        foreach (var cat in categories)
            if (!await _context.Categories.AnyAsync(c => c.CategoryName == cat.CategoryName))
                await _context.Categories.AddAsync(cat);
        await _context.SaveChangesAsync();

        // 2️⃣ Lấy Supplier
        var supplierUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "supplier@uniseapshop.com");
        if (supplierUser == null)
            throw new Exception("Supplier user chưa được seed. Hãy chạy SeedRolesAndUsers() trước.");

        var supplier = await _context.Suppliers.Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == supplierUser.Id);
        if (supplier == null)
            throw new Exception("Supplier entity chưa được seed. Hãy đảm bảo có Supplier cho user này.");

        // 3️⃣ Tạo danh sách sản phẩm
        var products = new List<Product>();

        // Helper function tạo ảnh
        List<ProductImage> CreateImages(Product p, string main, string sub)
        {
            var imgs = new List<ProductImage>
            {
                new() { ImageUrl = main, IsMainImage = true, Product = p, ProductId = p.Id },
                new() { ImageUrl = sub, Product = p, ProductId = p.Id }
            };
            return imgs;
        }

        // Sản phẩm 1
        var p1 = new Product
        {
            ProductName = "Áo Hoodie Unisex",
            ProductImage = "https://example.com/images/hoodie_main.jpg",
            Description = "Áo hoodie second-hand, form rộng, chất cotton dày dặn.",
            Price = 250000,
            OriginalPrice = 500000,
            Quantity = 5,
            Category = categories.First(c => c.CategoryName == "Thời trang"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Đã sử dụng 6 tháng, còn rất mới.",
            EstimatedAge = 6,
            Brand = "H&M",
            Weight = 0.6,
            Dimensions = "70x55"
        };
        p1.Images = CreateImages(p1, "https://example.com/images/hoodie_main.jpg",
            "https://example.com/images/hoodie_side.jpg");

        // Sản phẩm 2
        var p2 = new Product
        {
            ProductName = "Tai nghe Bluetooth Sony WH-CH510",
            ProductImage = "https://example.com/images/headphone_main.jpg",
            Description = "Tai nghe second-hand, âm thanh trong trẻo, pin còn tốt.",
            Price = 750000,
            OriginalPrice = 1500000,
            Quantity = 3,
            Category = categories.First(c => c.CategoryName == "Điện tử"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Sử dụng 1 năm, hoạt động ổn định.",
            EstimatedAge = 12,
            Brand = "Sony",
            Weight = 0.2,
            Dimensions = "18x15x5"
        };
        p2.Images = CreateImages(p2, "https://example.com/images/headphone_main.jpg",
            "https://example.com/images/headphone_side.jpg");

        // Sản phẩm 3
        var p3 = new Product
        {
            ProductName = "Bàn ủi hơi nước Philips GC2998",
            ProductImage = "https://example.com/images/iron_main.jpg",
            Description = "Bàn ủi hơi nước second-hand, còn hoạt động tốt.",
            Price = 400000,
            OriginalPrice = 900000,
            Quantity = 2,
            Category = categories.First(c => c.CategoryName == "Đồ gia dụng"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Dùng 1 năm, không bị rò nước.",
            EstimatedAge = 12,
            Brand = "Philips",
            Weight = 1.2,
            Dimensions = "25x12x15"
        };
        p3.Images = CreateImages(p3, "https://example.com/images/iron_main.jpg",
            "https://example.com/images/iron_side.jpg");

        // Sản phẩm 4
        var p4 = new Product
        {
            ProductName = "Sách 'Atomic Habits'",
            ProductImage = "https://example.com/images/book_main.jpg",
            Description = "Sách second-hand, còn mới 95%, không bị rách.",
            Price = 90000,
            OriginalPrice = 180000,
            Quantity = 4,
            Category = categories.First(c => c.CategoryName == "Sách & Văn phòng phẩm"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Đọc 1 lần, không ghi chú.",
            EstimatedAge = 8,
            Brand = "NXB Thế Giới",
            Weight = 0.4,
            Dimensions = "21x14x2"
        };
        p4.Images = CreateImages(p4, "https://example.com/images/book_main.jpg",
            "https://example.com/images/book_side.jpg");

        // Sản phẩm 5
        var p5 = new Product
        {
            ProductName = "Vợt cầu lông Yonex Nanoray",
            ProductImage = "https://example.com/images/racket_main.jpg",
            Description = "Vợt cầu lông cũ, khung carbon bền, dây còn tốt.",
            Price = 350000,
            OriginalPrice = 800000,
            Quantity = 3,
            Category = categories.First(c => c.CategoryName == "Đồ thể thao"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Sử dụng khoảng 1 năm, còn khá mới.",
            EstimatedAge = 12,
            Brand = "Yonex",
            Weight = 0.1,
            Dimensions = "67x20"
        };
        p5.Images = CreateImages(p5, "https://example.com/images/racket_main.jpg",
            "https://example.com/images/racket_side.jpg");

        // Sản phẩm 6
        var p6 = new Product
        {
            ProductName = "Nhẫn bạc nữ Pandora",
            ProductImage = "https://example.com/images/ring_main.jpg",
            Description = "Nhẫn bạc second-hand, còn sáng, không trầy xước.",
            Price = 200000,
            OriginalPrice = 600000,
            Quantity = 1,
            Category = categories.First(c => c.CategoryName == "Trang sức & Phụ kiện"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Đeo vài lần, còn như mới.",
            EstimatedAge = 5,
            Brand = "Pandora",
            Weight = 0.02,
            Dimensions = "2x2"
        };
        p6.Images = CreateImages(p6, "https://example.com/images/ring_main.jpg",
            "https://example.com/images/ring_side.jpg");

        // Sản phẩm 7
        var p7 = new Product
        {
            ProductName = "Túi xách da nữ Zara",
            ProductImage = "https://example.com/images/bag_main.jpg",
            Description = "Túi da second-hand, còn mới 90%, không bong tróc.",
            Price = 420000,
            OriginalPrice = 950000,
            Quantity = 2,
            Category = categories.First(c => c.CategoryName == "Thời trang"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Đã sử dụng 8 tháng.",
            EstimatedAge = 8,
            Brand = "Zara",
            Weight = 0.8,
            Dimensions = "30x10x20"
        };
        p7.Images = CreateImages(p7, "https://example.com/images/bag_main.jpg",
            "https://example.com/images/bag_side.jpg");

        // Sản phẩm 8
        var p8 = new Product
        {
            ProductName = "Máy xay sinh tố Philips HR2100",
            ProductImage = "https://example.com/images/blender_main.jpg",
            Description = "Máy xay sinh tố cũ, còn hoạt động tốt.",
            Price = 280000,
            OriginalPrice = 700000,
            Quantity = 3,
            Category = categories.First(c => c.CategoryName == "Đồ gia dụng"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Dùng 1 năm, lưỡi dao còn sắc.",
            EstimatedAge = 12,
            Brand = "Philips",
            Weight = 1.5,
            Dimensions = "30x15x15"
        };
        p8.Images = CreateImages(p8, "https://example.com/images/blender_main.jpg",
            "https://example.com/images/blender_side.jpg");

        // Sản phẩm 9
        var p9 = new Product
        {
            ProductName = "Giày thể thao Nike Air Zoom",
            ProductImage = "https://example.com/images/shoe_main.jpg",
            Description = "Giày second-hand, còn mới 85%, đế nguyên.",
            Price = 550000,
            OriginalPrice = 1500000,
            Quantity = 4,
            Category = categories.First(c => c.CategoryName == "Đồ thể thao"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Đã mang 10 tháng.",
            EstimatedAge = 10,
            Brand = "Nike",
            Weight = 0.9,
            Dimensions = "28x10x10"
        };
        p9.Images = CreateImages(p9, "https://example.com/images/shoe_main.jpg",
            "https://example.com/images/shoe_side.jpg");

        // Sản phẩm 10
        var p10 = new Product
        {
            ProductName = "Đồng hồ Casio MTP-1302D",
            ProductImage = "https://example.com/images/watch_main.jpg",
            Description = "Đồng hồ nam second-hand, còn nguyên hộp.",
            Price = 900000,
            OriginalPrice = 1800000,
            Quantity = 2,
            Category = categories.First(c => c.CategoryName == "Trang sức & Phụ kiện"),
            Supplier = supplier,
            Condition = ProductCondition.Good,
            UsageHistory = "Sử dụng 6 tháng, pin còn tốt.",
            EstimatedAge = 6,
            Brand = "Casio",
            Weight = 0.15,
            Dimensions = "4x4x1"
        };
        p10.Images = CreateImages(p10, "https://example.com/images/watch_main.jpg",
            "https://example.com/images/watch_side.jpg");

        products.AddRange(new[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 });

        // 4️⃣ Seed vào DB
        foreach (var p in products)
            if (!await _context.Products.AnyAsync(x => x.ProductName == p.ProductName))
                await _context.Products.AddAsync(p);

        await _context.SaveChangesAsync();

        // 5️⃣ Trả kết quả tóm tắt
        return products.Select(p => new
        {
            p.ProductName,
            Category = p.Category.CategoryName,
            Supplier = supplier,
            p.Price,
            ImageCount = p.Images.Count
        }).Cast<object>().ToList();
    }
}