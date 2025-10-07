using System.ComponentModel;

namespace UniSeapShop.Domain.DTOs.AuthenDTOs;

public class LoginRequestDto
{
    [DefaultValue("customer@uniseapshop.com")]
    public string? Email { get; set; }

    [DefaultValue("Customer123!")] public string? Password { get; set; }
}