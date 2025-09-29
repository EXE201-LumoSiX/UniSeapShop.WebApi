using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Domain.DTOs.AuthenDTOs;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutheController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IClaimsService _claimsService;
        private readonly IConfiguration _configuration;

        public AutheController(IAuthService authService, IClaimsService claimsService, IConfiguration configuration)
        {
            _authService = authService;
            _claimsService = claimsService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto, _configuration);
            if (result == null)
                return Unauthorized(new { Message = "Invalid credentials" });
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
        {
            var result = await _authService.RegisterCustomerAsync(registrationDto);
            if (result == null)
                return BadRequest(new { Message = "Registration failed" });
            return Ok(result);
        }
    }
}
