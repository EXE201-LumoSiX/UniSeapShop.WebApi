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
            if (await UserExistsAsync(registrationDto.Email))
                throw ErrorHelper.Conflict("Email already registered.");

            var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

            var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.RoleType == RoleType.Customer);
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

            _logger.Success($"User created successfully: {user.Email}");
            return ToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in CreateUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        try {
            var user = await _unitOfWork.Users.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                throw ErrorHelper.NotFound("User not found.");

            _logger.Info($"User retrieved successfully: {user.Email}");
            return ToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GetUserByIdAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _unitOfWork.Users.GetQueryable().Where(u => !u.IsDeleted).ToListAsync();
            _logger.Info($"Total users retrieved: {users.Count}");
            return users.Select(ToUserDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GetAllUsersAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UserUpdateDto updateDto)
    {
        try
        {
            var user = await _unitOfWork.Users.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                throw ErrorHelper.NotFound("User not found.");

            user.FullName = updateDto.FullName ?? user.FullName;
            user.PhoneNumber = updateDto.PhoneNumber ?? user.PhoneNumber;
            user.UserImage = updateDto.UserImage ?? user.UserImage;

            if (!string.IsNullOrWhiteSpace(updateDto.Password))
            {
                var hashedPassword = new PasswordHasher().HashPassword(updateDto.Password);
                user.Password = hashedPassword;
            }

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User updated successfully: {user.Email}");
            return ToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in UpdateUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                throw ErrorHelper.NotFound("User not found.");

            user.IsDeleted = true;
            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User deleted successfully: {user.Email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DeleteUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<UserDto?> GetCurrentUserProfileAsync()
    {
        try
        {
            var currentUserId = _claimService.CurrentUserId;
            var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, u => u.Role);
            if (user == null || user.IsDeleted)
                throw ErrorHelper.NotFound("User not found.");

            _logger.Info($"Current user profile retrieved: {user.Email}");
            return ToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GetCurrentUserProfileAsync: {ex.Message}");
            throw;
        }
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
            RoleName = user.Role?.RoleType.ToString()
        };
    }

    private async Task<bool> UserExistsAsync(string email)
    {
        try
        {
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            return existingUser != null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in UserExistsAsync: {ex.Message}");
            throw;
        }
    }
}