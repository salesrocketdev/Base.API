using Base.Domain.Entities;

namespace Base.Domain.Interfaces.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string email, string password, string? name);
    Task<(string nextStep, string maskedEmail)> InitiateLoginAsync(string email);
    Task<(User user, string accessToken, string refreshToken)> LoginAsync(string email, string password, string ipAddress, string userAgent);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue);
    Task<string> SwitchOrganizationAsync(int userId, Guid organizationPublicId);
    Task LogoutAsync(int userId, string refreshTokenValue);
    Task<User?> GetCurrentUserAsync(int userId);
    Task InitiatePasswordResetAsync(string email);
    Task ResetPasswordAsync(string email, string otp, string newPassword);
    Task<(User user, string accessToken, string refreshToken)> CompleteFirstAccessAsync(string email, string otp, string newPassword, string firstName, string lastName, string ipAddress, string userAgent);
    Task ResendFirstAccessOtpAsync(string email);
}


