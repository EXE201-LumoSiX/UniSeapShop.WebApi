using Microsoft.Extensions.Configuration;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;

namespace UniSeapShop.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);
    Task<UserDto?> RegisterCustomerAsync(UserRegistrationDto registrationDto);
}