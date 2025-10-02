using Microsoft.Extensions.Configuration;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;
using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);
    Task<UserDto?> RegisterCustomerAsync(UserRegistrationDto registrationDto);
    Task<bool> VerifyEmailOtpAsync(string email, string otp);
    Task<bool> ResendOtpAsync(string email, OtpType type);
}