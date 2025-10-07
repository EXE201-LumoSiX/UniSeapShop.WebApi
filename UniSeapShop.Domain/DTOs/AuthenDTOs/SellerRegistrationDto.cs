using System.ComponentModel;

namespace UniSeapShop.Domain.DTOs.AuthenDTOs;

public class SellerRegistrationDto
{
    [DefaultValue("test@gmail.com")] public required string Email { get; set; }

    public string? Description { get; set; }
    public float Rating { get; set; } = 0;
    public bool IsActive { get; set; } = false;
}