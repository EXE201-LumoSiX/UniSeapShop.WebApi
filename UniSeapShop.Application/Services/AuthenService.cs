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

public class AuthenService : IAuthService
{
    private readonly ICacheService _cacheService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

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

    public async Task<UserDto?> RegisterCustomerAsync(UserRegistrationDto registrationDto)
    {
        _logger.Info($"[RegisterUserAsync] Start registration for {registrationDto.Email}");

        if (await UserExistsAsync(registrationDto.Email))
        {
            _logger.Warn($"[RegisterUserAsync] Email {registrationDto.Email} already registered.");
            throw new Exception();
        }

        var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

        var user = new User
        {
            Email = registrationDto.Email,
            Password = hashedPassword,
            Username = registrationDto.FullName,
            PhoneNumber = registrationDto.PhoneNumber,
            RoleName = RoleType.Customer
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        //_logger.Success($"[RegisterUserAsync] User {user.Email} created successfully.");

        //await GenerateAndSendOtpAsync(user, OtpPurpose.Register, "register-otp");

        //_logger.Info($"[RegisterUserAsync] OTP sent to {user.Email} for verification.");

        return ToUserDto(user);
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

    //public async Task<UserDto?> RegisterSellerAsync(SellerRegistrationDto dto)
    //{
    //    if (await UserExistsAsync(dto.Email))
    //        throw ErrorHelper.Conflict(ErrorMessages.AccountEmailAlreadyRegistered);

    //    var hashedPassword = new PasswordHasher().HashPassword(dto.Password);

    //    var user = new User
    //    {
    //        Email = dto.Email,
    //        Password = hashedPassword,
    //        FullName = dto.FullName,
    //        Phone = dto.PhoneNumber,
    //        DateOfBirth = dto.DateOfBirth,
    //        AvatarUrl =
    //            "https://thumbs.dreamstime.com/b/faceless-businessman-avatar-man-suit-blue-tie-human-profile-userpic-face-features-web-picture-gentlemen-85824471.jpg",
    //        RoleName = RoleType.Seller,
    //        Status = UserStatus.Pending,
    //        IsEmailVerified = false
    //    };

    //    await _unitOfWork.Users.AddAsync(user);
    //    await _unitOfWork.SaveChangesAsync();

    //    var seller = new Seller
    //    {
    //        UserId = user.Id,
    //        CoaDocumentUrl = "Waiting for submit",
    //        CompanyName = dto.CompanyName,
    //        TaxId = dto.TaxId,
    //        CompanyAddress = dto.CompanyAddress,
    //        IsVerified = false,
    //        Status = SellerStatus.InfoEmpty
    //    };

    //    await _unitOfWork.Sellers.AddAsync(seller);
    //    await _unitOfWork.SaveChangesAsync();

    //    await GenerateAndSendOtpAsync(user, OtpPurpose.Register, "register-otp");

    //    return ToUserDto(user);
    //}
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

    private static UserDto ToUserDto(User user)
    {
        //if (user.RoleName.Equals(RoleType.Seller))
        //    return new UserDto
        //    {
        //        UserId = user.Id,
        //        Username = user.Username,
        //        Email = user.Email,
        //        DateOfBirth = user.DateOfBirth,
        //        AvatarUrl = user.AvatarUrl,
        //        Status = user.Status,
        //        PhoneNumber = user.PhoneNumber,
        //        RoleName = user.RoleName
        //    };


        return new UserDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleName = user.RoleName
        };
    }
}