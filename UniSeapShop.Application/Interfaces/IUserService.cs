using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;

namespace UniSeapShop.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> CreateUserAsync(UserRegistrationDto registrationDto);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> UpdateUserAsync(Guid userId, UserUpdateDto updateDto);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<UserDto?> GetCurrentUserProfileAsync();
    Task<SupplierDetailsDto?> GetSupplierByIdAsync(Guid supplierId);
    Task<SupplierDetailsDto?> UpdateSupplierBank(SupplierUpdate dto);
}