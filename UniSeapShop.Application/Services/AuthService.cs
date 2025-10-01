using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.EmailDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Domain.Enums;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class AuthService : IAuthService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;

    public AuthService(
        ILoggerService logger,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IEmailService emailService
    )
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _emailService = emailService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
    {
        var user = await GetUserByEmailAsync(loginDto.Email!);

        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (user.Role == null)
            throw ErrorHelper.NotFound("User role not found. Please contact admin.");

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
            IsActive = false // Chưa kích hoạt cho đến khi xác thực email
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.Success($"[RegisterUserAsync] User {user.Email} created successfully.");

        await GenerateAndSendOtpAsync(user, OtpPurpose.Register, "register-otp");

        _logger.Info($"[RegisterUserAsync] OTP sent to {user.Email} for verification.");

        return ToUserDto(user);
    }

    public async Task<bool> VerifyEmailOtpAsync(string email, string otp)
    {
        _logger.Info($"[VerifyEmailOtpAsync] Verifying OTP for {email}");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (user.IsEmailVerify) return false;
        if (!await VerifyOtpAsync(email, otp, OtpPurpose.Register, "register-otp"))
            return false;

        // Activate user
        user.IsEmailVerify = true;
        user.IsActive = true;
        var customer = CreateCustomer(user);


        await _unitOfWork.Users.Update(user);
        await _unitOfWork.Customers.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        // Xóa cache cũ rồi thiết lập lại cache user mới
        await _cacheService.RemoveAsync($"user:{email}");
        await _cacheService.SetAsync($"user:{email}", user, TimeSpan.FromHours(1));

        // Xóa OTP khỏi cache
        await _cacheService.RemoveAsync($"register-otp:{email}");


        await _emailService.SendRegistrationSuccessEmailAsync(new EmailRequestDto
        {
            To = user.Email,
            UserName = user.FullName
        });
        _logger.Success($"[VerifyEmailOtpAsync] User {email} verified and activated.");
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string otp, string newPassword)
    {
        _logger.Info($"[ResetPasswordAsync] Password reset requested for {email}");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        if (user == null) return false;
        if (!user.IsEmailVerify) return false;
        if (!await VerifyOtpAsync(email, otp, OtpPurpose.ForgotPassword, "forgot-otp"))
            return false;

        // Hash và cập nhật mật khẩu
        user.Password = new PasswordHasher().HashPassword(newPassword);
        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // ——> Xóa cache cũ rồi set lại cache user với mật khẩu mới
        await _cacheService.RemoveAsync($"user:{email}");
        await _cacheService.SetAsync($"user:{email}", user, TimeSpan.FromHours(1));

        // Xóa OTP khỏi cache
        await _cacheService.RemoveAsync($"forgot-otp:{email}");

        await _emailService.SendPasswordChangeEmailAsync(new EmailRequestDto
        {
            To = user.Email,
            UserName = user.FullName
        });

        _logger.Success($"[ResetPasswordAsync] Password reset successful for {email}.");
        return true;
    }

    public async Task<bool> ResendOtpAsync(string email, OtpType type)
    {
        return type switch
        {
            OtpType.Register => await ResendRegisterOtpAsync(email),
            OtpType.ForgotPassword => await SendForgotPasswordOtpRequestAsync(email),
            _ => throw ErrorHelper.BadRequest(ErrorMessages.Oauth_InvalidOtp)
        };
    }

    private async Task<bool> ResendRegisterOtpAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (user.IsDeleted)
            throw ErrorHelper.Forbidden(ErrorMessages.AccountSuspendedOrBan);

        if (user.IsEmailVerify)
            throw ErrorHelper.Conflict(ErrorMessages.AccountAlreadyVerified);

        if (await _cacheService.ExistsAsync($"otp-sent:{email}"))
            throw ErrorHelper.BadRequest(ErrorMessages.VerifyOtpExistingCoolDown);

        await GenerateAndSendOtpAsync(user, OtpPurpose.Register, "register-otp");
        await _cacheService.SetAsync($"otp-sent:{email}", true, TimeSpan.FromMinutes(1));

        return true;
    }

    private async Task<bool> SendForgotPasswordOtpRequestAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);

        if (user.IsDeleted)
            throw ErrorHelper.Forbidden(ErrorMessages.AccountSuspendedOrBan);

        if (!user.IsEmailVerify)
            throw ErrorHelper.Conflict(ErrorMessages.AccountNotVerified);

        var counterKey = $"forgot-otp-count:{email}";
        var countValue = await _cacheService.GetAsync<int?>(counterKey) ?? 0;

        if (countValue >= 3)
            throw ErrorHelper.BadRequest(ErrorMessages.Oauth_InvalidOtp);

        // Gửi OTP
        await GenerateAndSendOtpAsync(user, OtpPurpose.ForgotPassword, "forgot-otp");

        // Tăng số lần gửi và set timeout nếu là lần đầu tiên
        await _cacheService.SetAsync(counterKey, countValue + 1, TimeSpan.FromMinutes(15));


        _logger.Info($"[SendForgotPasswordOtpRequestAsync] OTP sent to {email}");

        return true;
    }

    private Customer CreateCustomer(User user)
    {
        return new Customer
        {
            UserId = user.Id,
            LoyaltyPoint = 0,
            MembershipLevel = "Basic",
            User = user
        };
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
        // Sử dụng GetQueryable để lấy user kèm Role
        var user = await _unitOfWork.Users
            .GetQueryable()
            .Where(u => u.Email == email && !u.IsDeleted)
            .Include(u => u.Role)
            .FirstOrDefaultAsync();
        if (user == null)
            throw ErrorHelper.NotFound(ErrorMessages.AccountNotFound);
        return user;
    }

    private async Task<bool> UserExistsAsync(string email)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }
    private async Task GenerateAndSendOtpAsync(User user, OtpPurpose purpose, string otpCachePrefix)
    {
        var otpToken = OtpGenerator.GenerateToken(6, TimeSpan.FromMinutes(10));
        var otp = new OtpVerification
        {
            Target = user.Email,
            OtpCode = otpToken.Code,
            ExpiredAt = otpToken.ExpiresAtUtc,
            IsUsed = false,
            Purpose = purpose
        };

        await _unitOfWork.OtpVerifications.AddAsync(otp);
        await _unitOfWork.SaveChangesAsync();
        await _cacheService.SetAsync($"{otpCachePrefix}:{user.Email}", otpToken.Code, TimeSpan.FromMinutes(10));

        // Send the correct email based on OTP purpose
        if (purpose == OtpPurpose.Register)
        {
            await _emailService.SendOtpVerificationEmailAsync(new EmailRequestDto
            {
                To = user.Email,
                Otp = otpToken.Code,
                UserName = user.FullName
            });
            _logger.Info($"[GenerateAndSendOtpAsync] Registration OTP sent to {user.Email}");
        }
        else if (purpose == OtpPurpose.ForgotPassword)
        {
            await _emailService.SendForgotPasswordOtpEmailAsync(new EmailRequestDto
            {
                To = user.Email,
                Otp = otpToken.Code,
                UserName = user.FullName
            });
            _logger.Info($"[GenerateAndSendOtpAsync] Forgot password OTP sent to {user.Email}");
        }
    }
    private async Task<bool> VerifyOtpAsync(string email, string otp, OtpPurpose purpose, string otpCachePrefix)
    {
        var cacheKey = $"{otpCachePrefix}:{email}";
        var cachedOtp = await _cacheService.GetAsync<string>(cacheKey);
        if (cachedOtp != null)
        {
            if (cachedOtp != otp)
            {
                _logger.Warn($"[VerifyOtpAsync] OTP mismatch for {email} (purpose: {purpose})");
                return false;
            }

            // Remove OTP from cache after successful verification
            await _cacheService.RemoveAsync(cacheKey);
            _logger.Info(
                $"[VerifyOtpAsync] OTP for {email} (purpose: {purpose}) verified and removed from cache.");
            return true;
        }

        // Fallback: check in DB if not found in cache
        var otpRecord = await _unitOfWork.OtpVerifications.FirstOrDefaultAsync(o =>
            o.Target == email && o.OtpCode == otp && o.Purpose == purpose && !o.IsUsed);

        if (otpRecord == null || otpRecord.ExpiredAt < DateTime.UtcNow)
        {
            _logger.Warn($"[VerifyOtpAsync] OTP not found or expired for {email} (purpose: {purpose})");
            return false;
        }

        otpRecord.IsUsed = true;
        await _unitOfWork.OtpVerifications.Update(otpRecord);
        await _unitOfWork.SaveChangesAsync();
        _logger.Info(
            $"[VerifyOtpAsync] OTP for {email} (purpose: {purpose}) verified and marked as used in DB.");
        return true;
    }
}