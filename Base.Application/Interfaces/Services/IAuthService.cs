using Base.Domain.Entities;

namespace Base.Application.Interfaces.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string email, string password, string? name);
    Task<(string nextStep, string maskedEmail)> InitiateLoginAsync(string email);
    Task<(User user, string accessToken, string refreshToken)> LoginAsync(string email, string password, string ipAddress, string userAgent);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue);
    Task LogoutAsync(int currentUserId, string refreshTokenValue);
    Task<User?> GetCurrentUserAsync(int userId);
    Task InitiatePasswordResetAsync(string email);
    Task ResetPasswordAsync(string email, string otp, string newPassword);
    Task<(User user, string accessToken, string refreshToken)> CompleteFirstAccessAsync(string email, string otp, string newPassword, string firstName, string lastName, string ipAddress, string userAgent);
    Task ResendFirstAccessOtpAsync(string email);
}
