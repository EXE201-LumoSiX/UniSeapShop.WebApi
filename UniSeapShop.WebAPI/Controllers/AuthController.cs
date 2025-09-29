using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Domain.DTOs.AuthenDTOs;

namespace UniSeapShop.WebAPI.Controllers;

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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto, _configuration);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest();
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterCustomer([FromBody] UserRegistrationDto dto)
    {
        try
        {
            var result = await _authService.RegisterCustomerAsync(dto);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest();
        }
    }
}