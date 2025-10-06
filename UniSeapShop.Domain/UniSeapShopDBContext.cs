using Microsoft.EntityFrameworkCore;
using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Domain;

public class UniSeapShopDBContext : DbContext
{
    public UniSeapShopDBContext()
    {
    }

    public UniSeapShopDBContext(DbContextOptions<UniSeapShopDBContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<OtpVerification> OtpVerifications { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PayoutDetail> PayoutDetails { get; set; }
    public DbSet<Feeback> Feedbacks { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.RoleType).IsRequired();

            // One-to-Many relationship with Users
            entity.HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of users when a role is deleted
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.PhoneNumber).IsRequired();
            entity.Property(e => e.RoleId).IsRequired();

            // Many-to-One relationship with Role already defined in Role config

            // One-to-One relationship with Customer
            entity.HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-One relationship with Supplier
            entity.HasOne(u => u.Supplier)
                .WithOne(s => s.User)
                .HasForeignKey<Supplier>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();

            // One-to-Many relationship with Orders
            entity.HasMany(c => c.Orders)
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-One relationship with Cart
            entity.HasOne(c => c.Cart)
                .WithOne(cart => cart.Customer)
                .HasForeignKey<Cart>(cart => cart.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Supplier configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();

            // One-to-Many relationship with Products
            entity.HasMany(s => s.Products)
                .WithOne(p => p.Supplier)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid multiple cascade paths
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CategoryName).IsRequired();

            // One-to-Many relationship with Products
            entity.HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid multiple cascade paths
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired();
            entity.Property(e => e.Price).IsRequired();
            entity.Property(e => e.CategoryId).IsRequired();
            entity.Property(e => e.SupplierId).IsRequired();

            // Many-to-One relationship with Category already defined in Category config

            // Many-to-One relationship with Supplier already defined in Supplier config

            // One-to-Many relationship with OrderDetails
            entity.HasMany(p => p.OrderDetails)
                .WithOne(od => od.Product)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deletion if product is in an order

            // One-to-Many relationship with CartItems
            entity.HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid multiple cascade paths

            // One-to-Many relationship with ProductImages
            entity.HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductImage configuration
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.ImageUrl).IsRequired();
        });

        // Cart configuration
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();

            // One-to-Many relationship with CartItems
            entity.HasMany(c => c.CartItems)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CartItem configuration
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.CartId).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.ShipAddress).IsRequired();
            entity.Property(e => e.PaymentMethod).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            // One-to-Many relationship with OrderDetails
            entity.HasMany(o => o.OrderDetails)
                .WithOne(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-One relationship with Payment
            entity.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-One relationship with PayoutDetail
            entity.HasOne(o => o.PayoutDetail)
                .WithOne(p => p.Order)
                .HasForeignKey<PayoutDetail>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderDetail configuration
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).IsRequired();
            entity.Property(e => e.TotalPrice).IsRequired();

            // Many-to-One relationship with Order already defined in Order config

            // Many-to-One relationship with Product already defined in Product config

            // Many-to-One relationship with Voucher (optional)
            entity.HasOne(od => od.Voucher)
                .WithMany(v => v.OrderDetails)
                .HasForeignKey(od => od.VoucherId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // One-to-One relationship with Feedback
            entity.HasOne(od => od.Feeback)
                .WithOne(f => f.OrderDetail)
                .HasForeignKey<Feeback>(f => f.OrderDetailId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.TransactionCode).IsRequired();
        });

        // PayoutDetail configuration
        modelBuilder.Entity<PayoutDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceiverId).IsRequired();
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.TotalPrice).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ActualReceipt).IsRequired();
        });

        // Feedback configuration
        modelBuilder.Entity<Feeback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderDetailId).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Rating).IsRequired();
        });

        // Voucher configuration
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.DiscountPercent).IsRequired();
            entity.Property(e => e.ExpiryDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.Property(e => e.Purpose)
                .HasConversion<string>() // enum -> string
                .HasMaxLength(32); // giới hạn độ dài nếu cần
        });
    }
}