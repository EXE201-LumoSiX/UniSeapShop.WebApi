using System.ComponentModel.DataAnnotations;

namespace UniSeapShop.Domain.Entities;

public class User : BaseEntity
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string PhoneNumber { get; set; }
    public string UserImage { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsEmailVerify { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid RoleId { get; set; }
    public string CurrentMode { get; set; } = string.Empty;

    [MaxLength(128)] public string? RefreshToken { get; set; }
    [MaxLength(128)] public DateTime? RefreshTokenExpiryTime { get; set; }

    // Navigation properties
    public required Role Role { get; set; }
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
}