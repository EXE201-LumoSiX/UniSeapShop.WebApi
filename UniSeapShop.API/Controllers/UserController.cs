using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Utils;
using UniSeapShop.Domain.DTOs.UserDTOs;

namespace UniSeapShop.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var result = await _userService.GetCurrentUserProfileAsync();
            return Ok(ApiResult<UserDto>.Success(result!, "200", "Get profile successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var result = await _userService.GetUserByIdAsync(id);
            return Ok(ApiResult<UserDto>.Success(result!, "200", "Get user by id successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(ApiResult<List<UserDto>>.Success(result!, "200", "Get all users successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<UserDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto updateDto)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, updateDto);
            return Ok(ApiResult<UserDto>.Success(result!, "200", "Update user successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);
            return Ok(ApiResult<bool>.Success(result, "200", "Delete user successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<bool>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("supplier/{id}")]
    [Authorize]
    public async Task<IActionResult> GetSupplierById(Guid id)
    {
        try
        {
            var result = await _userService.GetSupplierByIdAsync(id);
            return Ok(ApiResult<SupplierDetailsDto>.Success(result!, "200", "Get supplier by id successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SupplierDetailsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
    [HttpPut("supplier")]
    [Authorize]
    public async Task<IActionResult> UpdateSupplierById([FromBody] SupplierUpdate dto)
    {
        try
        {
            var result = await _userService.UpdateSupplierBank(dto);
            return Ok(ApiResult<SupplierDetailsDto>.Success(result!, "200", "Get supplier by id successful"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<SupplierDetailsDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}