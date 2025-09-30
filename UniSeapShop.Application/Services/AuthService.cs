using Microsoft.Extensions.Configuration;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Domain.Enums;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class AuthService : IAuthService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        ILoggerService logger,
        IUnitOfWork unitOfWork
    )
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
    {
        _logger.Info($"[LoginAsync] Login attempt for {loginDto.Email}");

        // Lấy user từ DB trực tiếp, không dùng cache
        var user = await GetUserByEmailAsync(loginDto.Email!);

        // ✅ Check null sớm: nếu không tồn tại thì throw NotFound
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        // Nếu là Seller thì lấy thêm thông tin Seller
        //if (user.Role == RoleType.Seller)
        //{
        //    var seller = await GetSellerByUserIdAsync(user.Id);
        //    if (seller == null)
        //        throw ErrorHelper.NotFound("Không tìm thấy thông tin người dùng");

        //    user.Seller = seller;
        //}

        // Check account status
        if (!user.IsActive)
            throw ErrorHelper.Forbidden(ErrorMessages.AccountNotVerified);

        _logger.Success($"[LoginAsync] User {loginDto.Email} authenticated successfully.");
        // Get role
        string roleName = GetUserRole(user.RoleId);

        // Generate JWT & RefreshToken
        var accessToken = JwtUtils.GenerateJwtToken(
            user.Id,
            user.Email,
            roleName,
            configuration,
            TimeSpan.FromMinutes(30)
        );

        var refreshToken = Guid.NewGuid().ToString();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Không cache user nữa

        // Push welcome notification nếu chưa gửi
        //await SendWelcomeNotificationIfNotSentAsync(user);

        _logger.Info($"[LoginAsync] Tokens generated and user cache updated for {user.Email}");

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = ToUserDto(user)
        };
    }

    public async Task<UserDto?> RegisterCustomerAsync(UserRegistrationDto registrationDto)
    {
        if (await UserExistsAsync(registrationDto.Email))
            throw ErrorHelper.Conflict(ErrorMessages.AccountEmailAlreadyRegistered);

        var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

        var user = new User
        {
            Email = registrationDto.Email,
            Password = hashedPassword,
            FullName = registrationDto.FullName,
            PhoneNumber = registrationDto.PhoneNumber,
            UserImage = "https://img.freepik.com/free-psd/3d-illustration-human-avatar-profile_23-2150671142.jpg",
            Role = new Role
            {
                RoleType = RoleType.Customer,
                Name = "Customer"
            }, // Mặc định là Customer
            IsEmailVerify = false,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ToUserDto(user);
    }


    private static UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            UserId = user.Id,
            Username = user.FullName,
            Email = user.Email,
            UserImage = user.UserImage,
            PhoneNumber = user.PhoneNumber
        };
    }

    private string GetUserRole(Guid id)
    {
        var roleName = _unitOfWork
            .Where<Role>(u => u.Id == id)
            .Select(u => u.Name) // hoặc cột Role trực tiếp
            .FirstOrDefault();

        return roleName ?? string.Empty;
    }

    private async Task<User?> GetUserByEmailAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);
        return user;
    }

    private async Task<bool> UserExistsAsync(string email)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }
}