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
    private readonly ICacheService _cacheService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        ICacheService cacheService,
        ILoggerService logger,
        IUnitOfWork unitOfWork
    )
    {
        _cacheService = cacheService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
    {
        _logger.Info($"[LoginAsync] Login attempt for {loginDto.Email}");

        // Lấy user từ DB (không dùng cache ở bước đầu để đảm bảo tính chính xác)
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

        // Generate JWT & RefreshToken
        var accessToken = JwtUtils.GenerateJwtToken(
            user.Id,
            user.Email,
            user.Role.RoleType.ToString(),
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
        _logger.Info($"[RegisterUserAsync] Start registration for {registrationDto.Email}");

        if (await UserExistsAsync(registrationDto.Email))
        {
            _logger.Warn($"[RegisterUserAsync] Email {registrationDto.Email} already registered.");
            throw ErrorHelper.Conflict(ErrorMessages.AccountEmailAlreadyRegistered);
        }

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

        _logger.Success($"[RegisterUserAsync] User {user.Email} created successfully.");

        _logger.Info($"[RegisterUserAsync] OTP sent to {user.Email} for verification.");

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
            PhoneNumber = user.PhoneNumber,
            RoleName = user.Role.RoleType
        };
    }

    private async Task<User?> GetUserByEmailAsync(string email, bool useCache = false)
    {
        if (useCache)
        {
            var cacheKey = $"user:{email}";
            var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
            if (cachedUser != null) return cachedUser;

            var userFromDb = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
            if (userFromDb == null)
                throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

            await _cacheService.SetAsync(cacheKey, userFromDb, TimeSpan.FromHours(1));
            return userFromDb;
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        // ✅ Bắt buộc throw NotFound nếu null
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        return user;
    }

    private async Task<bool> UserExistsAsync(string email)
    {
        var cacheKey = $"user:{email}";
        var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
        if (cachedUser != null) return true;

        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            await _cacheService.SetAsync(cacheKey, existingUser, TimeSpan.FromHours(1));
            return true;
        }

        return false;
    }

    //private async Task GenerateAndSendOtpAsync(User user, OtpPurpose purpose, string otpCachePrefix)
    //{
    //    var otpToken = OtpGenerator.GenerateToken(6, TimeSpan.FromMinutes(10));
    //    var otp = new OtpVerification
    //    {
    //        Target = user.Email,
    //        OtpCode = otpToken.Code,
    //        ExpiredAt = otpToken.ExpiresAtUtc,
    //        IsUsed = false,
    //        Purpose = purpose
    //    };

    //    await _unitOfWork.OtpVerifications.AddAsync(otp);
    //    await _unitOfWork.SaveChangesAsync();
    //    await _cacheService.SetAsync($"{otpCachePrefix}:{user.Email}", otpToken.Code, TimeSpan.FromMinutes(10));

    //    // Send the correct email based on OTP purpose
    //    if (purpose == OtpPurpose.Register)
    //    {
    //        await _emailService.SendOtpVerificationEmailAsync(new EmailRequestDto
    //        {
    //            To = user.Email,
    //            Otp = otpToken.Code,
    //            UserName = user.FullName
    //        });
    //        _logger.Info($"[GenerateAndSendOtpAsync] Registration OTP sent to {user.Email}");
    //    }
    //    else if (purpose == OtpPurpose.ForgotPassword)
    //    {
    //        await _emailService.SendForgotPasswordOtpEmailAsync(new EmailRequestDto
    //        {
    //            To = user.Email,
    //            Otp = otpToken.Code,
    //            UserName = user.FullName
    //        });
    //        _logger.Info($"[GenerateAndSendOtpAsync] Forgot password OTP sent to {user.Email}");
    //    }
    //}
}