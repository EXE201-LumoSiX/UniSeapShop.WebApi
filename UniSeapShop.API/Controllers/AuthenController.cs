using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Domain.DTOs.UserDTOs;

namespace UniSeapShop.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthenController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

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
}