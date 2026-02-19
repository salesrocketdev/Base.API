using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(string email, string password, string? name);
    Task<(User user, string accessToken, string refreshToken)> LoginAsync(string email, string password, string ipAddress, string userAgent);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue);
    Task LogoutAsync(int currentUserId, string refreshTokenValue);
    Task<User?> GetCurrentUserAsync(int userId);
    Task InitiatePasswordResetAsync(string email);
    Task ResetPasswordAsync(string email, string otp, string newPassword);
}

