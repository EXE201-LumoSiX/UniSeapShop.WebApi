namespace UniSeapShop.Domain.DTOs.UserDTOs;

public class UserDto
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string UserImage { get; set; }
    public string? RoleName { get; set; }
}