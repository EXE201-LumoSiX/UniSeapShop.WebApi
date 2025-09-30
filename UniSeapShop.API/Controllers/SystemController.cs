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

    [HttpPost("seed-all-data")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await ClearDatabase(_context);

            // Seed data
            await SeedRolesAndUsers();
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

    private async Task SeedRolesAndUsers()
    {
        // Định nghĩa các role dựa trên README.md
        var roles = new List<Role>
        {
            new()
            {
                Name = "User",
                RoleType = RoleType.Customer,
                Description = "Người mua hàng, sử dụng các dịch vụ của hệ thống.",
                IsActive = true
            },
            new()
            {
                Name = "Supplier",
                RoleType = RoleType.Supplier,
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

        // Seed một số user mẫu cho từng role
        var users = new List<User>
        {
            new()
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
            new()
            {
                FullName = "Supplier User",
                Email = "supplier@uniseapshop.com",
                Password = new PasswordHasher().HashPassword("Supplier123!"),
                PhoneNumber = "0987654321",
                RoleId = roles.First(r => r.RoleType == RoleType.Supplier).Id,
                Role = roles.First(r => r.RoleType == RoleType.Supplier),
                IsEmailVerify = true,
                IsActive = true
            },
            new()
            {
                FullName = "Customer User",
                Email = "customer@uniseapshop.com",
                Password = new PasswordHasher().HashPassword("Customer123!"),
                PhoneNumber = "0111222333",
                RoleId = roles.First(r => r.RoleType == RoleType.Customer).Id,
                Role = roles.First(r => r.RoleType == RoleType.Customer),
                IsEmailVerify = true,
                IsActive = true
            }
        };

        foreach (var user in users)
            if (!await _context.Users.AnyAsync(u => u.Email == user.Email))
                await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}