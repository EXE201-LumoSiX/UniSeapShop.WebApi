namespace UniSeapShop.Domain.DTOs.UserDTOs;

public class UserUpdateDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? UserImage { get; set; }
    public string? Password { get; set; } // Nếu muốn cho phép đổi mật khẩu
}