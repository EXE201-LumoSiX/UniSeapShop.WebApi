using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;

namespace UniSeapShop.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    /// <summary>
    ///     Đăng nhập hệ thống bằng email và mật khẩu.
    /// </summary>
    /// <param name="loginDto">Thông tin đăng nhập (email, password).</param>
    /// <returns>Thông tin đăng nhập thành công hoặc lỗi.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
    {
        try
        {
            var result = await _authService.LoginAsync(loginDto, _configuration);
            return Ok(ApiResult<LoginResponseDto>.Success(result!, "200", "Login successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    ///     Đăng ký tài khoản mới cho người dùng.
    /// </summary>
    /// <param name="registrationDto">Thông tin đăng ký (email, password, tên, số điện thoại).</param>
    /// <returns>Thông tin tài khoản đã đăng ký hoặc lỗi.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
    {
        try
        {
            var result = await _authService.RegisterCustomerAsync(registrationDto);
            return Ok(ApiResult<object>.Success(result!, "200", "Registration successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var verified = await _authService.VerifyEmailOtpAsync(dto.Email, dto.Otp);
        if (!verified)
            return BadRequest(ApiResult.Failure("400", "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng thử lại."));

        return Ok(ApiResult.Success("200", "Xác thực thành công. Tài khoản của bạn đã được kích hoạt."));
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromForm] ResendOtpRequestDto dto)
    {
        try
        {
            var sent = await _authService.ResendOtpAsync(dto.Email, dto.Type);
            return Ok(ApiResult<object>.Success(sent!, "200",
                "Mã OTP đã được gửi lại thành công. Vui lòng kiểm tra email của bạn."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    // [HttpPost("reset-password")]
    // public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    // {
    //     var reset = await _authService.ResetPasswordAsync(dto.Email, dto.Otp, dto.NewPassword);
    //     if (!reset)
    //         return BadRequest(ApiResult.Failure("400",
    //             "Mã OTP không hợp lệ, đã hết hạn hoặc thông tin đặt lại mật khẩu không chính xác."));
    //     return Ok(ApiResult.Success("200", "Mật khẩu của bạn đã được đặt lại thành công."));
    // }
}