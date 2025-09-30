using System.ComponentModel;

namespace UniSeapShop.Domain.DTOs.AuthenDTOs;

public class LoginRequestDto
{
    [DefaultValue("test@email.com")]
    public string? Email { get; set; }
    [DefaultValue("Test123!")]
    public string? Password { get; set; }
}