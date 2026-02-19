using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Base.API.Constants;
using Base.API.DTOs;
using Base.Domain.Interfaces;
using System.Security.Claims;

namespace Base.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Email, request.Password, request.Name);
            return Ok(ApiResponse<object?>.Ok(null, "User registered successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object?>.Fail(
                ex.Message,
                new ApiError(ApiErrorCodes.InvalidOperation, ex.Message, ApiErrorTypes.Validation)
            ));
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ApiConstants.UnknownIpAddress; // TODO: Ensure proper IP extraction in production (consider X-Forwarded-For if behind proxy)
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var (user, accessToken, refreshToken) = await _authService.LoginAsync(request.Email, request.Password, ipAddress, userAgent);

            var response = new LoginResponse(
                accessToken,
                refreshToken,
                new UserSummary(user.PublicId, user.Email, user.Name, user.AvatarUrl)
            );

            return Ok(ApiResponse<LoginResponse>.Ok(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object?>.Fail(
                ex.Message,
                new ApiError(ApiErrorCodes.Unauthorized, ex.Message, ApiErrorTypes.Unauthorized)
            ));
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var (accessToken, refreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<RefreshResponse>.Ok(new RefreshResponse(accessToken, refreshToken)));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object?>.Fail(
                ex.Message,
                new ApiError(ApiErrorCodes.Unauthorized, ex.Message, ApiErrorTypes.Unauthorized)
            ));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<object?>.Fail(
                "Unauthorized",
                new ApiError(ApiErrorCodes.Unauthorized, "Unauthorized", ApiErrorTypes.Unauthorized)
            ));
        }

        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Ok(ApiResponse<object?>.Ok(null, "Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<object?>.Fail(
                "Unauthorized",
                new ApiError(ApiErrorCodes.Unauthorized, "Unauthorized", ApiErrorTypes.Unauthorized)
            ));
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "User not found",
                new ApiError(ApiErrorCodes.UserNotFound, "User not found", ApiErrorTypes.NotFound)
            ));
        }

        return Ok(ApiResponse<UserSummary>.Ok(new UserSummary(user.PublicId, user.Email, user.Name, user.AvatarUrl)));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.InitiatePasswordResetAsync(request.Email);
        return Ok(ApiResponse<object?>.Ok(null, "If the email exists, an OTP code has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request.Email, request.Otp, request.NewPassword);
            return Ok(ApiResponse<object?>.Ok(null, "Password reset successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object?>.Fail(
                ex.Message,
                new ApiError(ApiErrorCodes.InvalidOperation, ex.Message, ApiErrorTypes.Validation)
            ));
        }
    }
}


