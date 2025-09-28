using Microsoft.Extensions.Configuration;
using UniSeapShop.Domain.DTOs.AuthenDTOs;

namespace UniSeapShop.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);
    }
}
