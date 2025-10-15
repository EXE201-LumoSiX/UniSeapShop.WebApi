using Microsoft.EntityFrameworkCore;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Domain.Enums;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class UserService : IUserService
{
    private readonly IClaimsService _claimService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        ILoggerService logger,
        IUnitOfWork unitOfWork,
        IClaimsService claimService
    )
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _claimService = claimService;
    }

    public async Task<UserDto?> CreateUserAsync(UserRegistrationDto registrationDto)
    {
        if (await UserExistsAsync(registrationDto.Email))
            throw ErrorHelper.Conflict("Email already registered.");

        var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

        var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.RoleType == RoleType.User);
        if (role == null)
            throw ErrorHelper.NotFound("Default role 'Customer' not found.");

        var user = new User
        {
            Email = registrationDto.Email,
            Password = hashedPassword,
            FullName = registrationDto.FullName,
            PhoneNumber = registrationDto.PhoneNumber,
            UserImage = "https://img.freepik.com/free-psd/3d-illustration-human-avatar-profile_23-2150671142.jpg",
            IsEmailVerify = false,
            IsActive = true,
            RoleId = role.Id,
            Role = role
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ToUserDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            throw ErrorHelper.NotFound("User not found.");
        return ToUserDto(user);
    }
    public async Task<SupplierDetailsDto?> GetSupplierByIdAsync(Guid supplierId)
    {
        var supplier = await _unitOfWork.Suppliers.GetQueryable().FirstOrDefaultAsync(u => u.Id == supplierId);
        if (supplier == null)
            throw ErrorHelper.NotFound("User not found.");
        return new SupplierDetailsDto
        {
            Description = supplier.Description,
            Location = supplier.Location,
            Rating = supplier.Rating
        };
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetQueryable().Where(u => !u.IsDeleted).ToListAsync();
        return users.Select(ToUserDto).ToList();
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UserUpdateDto updateDto)
    {
        var user = await _unitOfWork.Users.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            throw ErrorHelper.NotFound("User not found.");

        user.FullName = updateDto.FullName ?? user.FullName;
        user.PhoneNumber = updateDto.PhoneNumber ?? user.PhoneNumber;
        user.UserImage = updateDto.UserImage ?? user.UserImage;
        // Nếu cần cập nhật password, thêm logic tại đây

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ToUserDto(user);
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            throw ErrorHelper.NotFound("User not found.");
        user.IsDeleted = true;
        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> GetCurrentUserProfileAsync()
    {
        var currentUserId = _claimService.CurrentUserId;
        var user = await _unitOfWork.Users.GetQueryable()
            .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);
        if (user == null)
            throw ErrorHelper.NotFound("User not found.");
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

    private async Task<bool> UserExistsAsync(string email)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }
}