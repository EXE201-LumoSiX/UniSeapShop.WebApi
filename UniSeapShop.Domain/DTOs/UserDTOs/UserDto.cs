namespace UniSeapShop.Domain.DTOs.UserDTOs;

public class UserDto
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string UserImage { get; set; }
<<<<<<< HEAD
    public string? RoleName { get; set; }
    public Guid RoleId { get; set; }
=======
    public RoleType? RoleName { get; set; }
>>>>>>> 70f9a2dacec1cf70cab8a22e6fb9c141f7de702e
    public Guid? SupplierId { get; set; }
}