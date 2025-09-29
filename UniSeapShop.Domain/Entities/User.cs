using System.ComponentModel.DataAnnotations;
using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; }
    public required string Email { get; set; }
    public string? Password { get; set; }
    public required string PhoneNumber { get; set; }
    public Role? Role { get; set; }
    public required RoleType RoleName { get; set; }
    [MaxLength(128)] public string? RefreshToken { get; set; }
    [MaxLength(128)] public DateTime? RefreshTokenExpiryTime { get; set; }
}