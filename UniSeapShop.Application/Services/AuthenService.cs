using Microsoft.Extensions.Configuration;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class AuthenService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;
        private readonly ICacheService _cacheService;
        public AuthenService(
            IUnitOfWork unitOfWork,
            ILoggerService logger,
            ICacheService cacheService
            )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
        }
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
        {
            _logger.Info($"[LoginAsync] Login attempt for {loginDto.Email}");

            // Lấy user từ DB (không dùng cache ở bước đầu để đảm bảo tính chính xác)
            var user = await GetUserByEmailAsync(loginDto.Email!);

            // ✅ Check null sớm: nếu không tồn tại thì throw NotFound
            if (user == null)
                throw new ArgumentException("ErrorMessages.AccountNotFound");

            _logger.Success($"[LoginAsync] User {loginDto.Email} authenticated successfully.");

            // Generate JWT & RefreshToken
            var accessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                user.RoleName.ToString(),
                configuration,
                TimeSpan.FromMinutes(30)
            );

            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Cache user
            await _cacheService.SetAsync($"user:{user.Email}", user, TimeSpan.FromHours(1));

            _logger.Info($"[LoginAsync] Tokens generated and user cache updated for {user.Email}");

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        private async Task<User?> GetUserByEmailAsync(string email, bool useCache = false)
        {
            if (useCache)
            {
                var cacheKey = $"user:{email}";
                var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
                if (cachedUser != null) return cachedUser;

                var userFromDb = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (userFromDb == null)
                    throw new Exception();

                await _cacheService.SetAsync(cacheKey, userFromDb, TimeSpan.FromHours(1));
                return userFromDb;
            }

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);

            // ✅ Bắt buộc throw NotFound nếu null
            if (user == null)
                throw new Exception();

            return user;
        }
    }
}
