using System.ComponentModel;

namespace UniSeapShop.Domain.DTOs.AuthenDTOs;

public class SellerRegistrationDto
{
    [DefaultValue("test@gmail.com")] public required string Email { get; set; }

    public string? Description { get; set; }
    public string? Location { get; set; }
    public string AccountBank { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}