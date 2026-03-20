using System.Security.Cryptography;
using Base.Core.Email;
using Base.Core.Email.Models;
using Base.Core.Helpers;
using Base.Core.Security;
using Base.Domain.Constants;
using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Application.Interfaces.Services;

namespace Base.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IPasswordResetOtpProtector _passwordResetOtpProtector;
    private readonly ISendMailService _sendMailService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenHasher tokenHasher,
        IPasswordResetOtpProtector passwordResetOtpProtector,
        ISendMailService sendMailService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenHasher = tokenHasher;
        _passwordResetOtpProtector = passwordResetOtpProtector;
        _sendMailService = sendMailService;
    }

    public async Task<User> RegisterAsync(string email, string password, string? name)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(email))
        {
            throw new InvalidOperationException("Unable to create account. Please try again.");
        }

        var company = new Company
        {
            Name = $"{name ?? email.Split('@')[0]}'s Company",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Companies.CreateAsync(company);

        var hashedPassword = _passwordHasher.HashPassword(password);
        var user = new User
        {
            Email = email.ToLower(),
            Name = name,
            IsActive = true,
            Company = company,
            AvatarUrl = AvatarHelper.GenerateUserAvatar(name ?? email.Split('@')[0])
        };

        await _unitOfWork.Users.CreateAsync(user);

        var credentials = new UserCredentials
        {
            User = user,
            PasswordHash = hashedPassword
        };

        await _unitOfWork.UserCredentials.CreateAsync(credentials);

        var companyMember = new CompanyMember
        {
            Company = company,
            User = user,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CompanyMembers.CreateAsync(companyMember);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditEvent(user.Id, AuditEventTypes.Signup, null, null);

        var welcomeModel = new WelcomeModel
        {
            Name = user.Name ?? "User",
            LastName = ""
        };
        _sendMailService.EnqueueWelcomeEmail(user.Email, welcomeModel);

        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<(string nextStep, string maskedEmail)> InitiateLoginAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user == null)
        {
            return ("unavailable", MaskEmail(email));
        }

        var credentials = await _unitOfWork.UserCredentials.GetByUserIdAsync(user.Id);
        if (credentials == null || string.IsNullOrWhiteSpace(credentials.PasswordHash))
        {
            await SendOtpAsync(user);
            return ("first_access", MaskEmail(user.Email));
        }

        return ("password", MaskEmail(user.Email));
    }

    public async Task<(User user, string accessToken, string refreshToken)> LoginAsync(string email, string password, string ipAddress, string userAgent)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user == null)
        {
            await LogAuditEvent(AuthConstants.UnknownUserId, AuditEventTypes.LoginFailed, ipAddress, userAgent);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var credentials = await _unitOfWork.UserCredentials.GetByUserIdAsync(user.Id);
        if (credentials == null || string.IsNullOrWhiteSpace(credentials.PasswordHash))
        {
            throw new InvalidOperationException("First access completion is required.");
        }

        var verificationResult = _passwordHasher.VerifyPassword(credentials.PasswordHash, password);
        if (!verificationResult.Succeeded)
        {
            await LogAuditEvent(user.Id, AuditEventTypes.LoginFailed, ipAddress, userAgent);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (verificationResult.NeedsRehash)
        {
            credentials.PasswordHash = _passwordHasher.HashPassword(password);
            await _unitOfWork.UserCredentials.UpdateAsync(credentials);
        }

        var (accessToken, refreshTokenValue) = await CreateSessionAsync(user, userAgent);

        await LogAuditEvent(user.Id, AuditEventTypes.Login, ipAddress, userAgent);
        await _unitOfWork.SaveChangesAsync();

        return (user, accessToken, refreshTokenValue);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue)
    {
        var tokenHash = _tokenHasher.HashToken(refreshTokenValue);
        var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(tokenHash);

        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);

        var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenHasher.HashToken(newRefreshTokenValue);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenLifetimeDays),
            DeviceInfo = refreshToken.DeviceInfo
        };

        await _unitOfWork.RefreshTokens.CreateAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return (newAccessToken, newRefreshTokenValue);
    }

    public async Task LogoutAsync(int currentUserId, string refreshTokenValue)
    {
        var tokenHash = _tokenHasher.HashToken(refreshTokenValue);
        var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(tokenHash);

        if (refreshToken != null && refreshToken.UserId == currentUserId)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);

            await LogAuditEvent(refreshToken.UserId, AuditEventTypes.Logout, null, null);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<User?> GetCurrentUserAsync(int userId)
    {
        return await _unitOfWork.Users.GetByIdAsync(userId);
    }

    public async Task InitiatePasswordResetAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user != null)
        {
            var credentials = await _unitOfWork.UserCredentials.GetByUserIdAsync(user.Id);
            if (credentials != null && !string.IsNullOrWhiteSpace(credentials.PasswordHash))
            {
                await SendOtpAsync(user);
            }
        }
    }

    public async Task ResetPasswordAsync(string email, string otp, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user == null)
        {
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        var tokenHash = _passwordResetOtpProtector.HashOtp(user.Id, otp);
        var resetToken = await _unitOfWork.PasswordResetTokens.GetByUserIdAndTokenHashAsync(user.Id, tokenHash);
        if (resetToken == null)
        {
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        var credentials = await _unitOfWork.UserCredentials.GetByUserIdAsync(user.Id);
        if (credentials == null)
        {
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        credentials.PasswordHash = _passwordHasher.HashPassword(newPassword);
        await _unitOfWork.UserCredentials.UpdateAsync(credentials);

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken);

        await _unitOfWork.RefreshTokens.RevokeAllTokensByUserIdAsync(user.Id);
        await LogAuditEvent(user.Id, AuditEventTypes.ResetPassword, null, null);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(User user, string accessToken, string refreshToken)> CompleteFirstAccessAsync(string email, string otp, string newPassword, string firstName, string lastName, string ipAddress, string userAgent)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user == null)
        {
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        var credentials = await _unitOfWork.UserCredentials.GetByUserIdAsync(user.Id);
        if (credentials != null && !string.IsNullOrWhiteSpace(credentials.PasswordHash))
        {
            throw new InvalidOperationException("First access has already been completed.");
        }

        var tokenHash = _passwordResetOtpProtector.HashOtp(user.Id, otp);
        var resetToken = await _unitOfWork.PasswordResetTokens.GetByUserIdAndTokenHashAsync(user.Id, tokenHash);
        if (resetToken == null)
        {
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        if (credentials == null)
        {
            credentials = new UserCredentials
            {
                UserId = user.Id,
                PasswordHash = _passwordHasher.HashPassword(newPassword)
            };
            await _unitOfWork.UserCredentials.CreateAsync(credentials);
        }
        else
        {
            credentials.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _unitOfWork.UserCredentials.UpdateAsync(credentials);
        }

        var fullName = $"{firstName.Trim()} {lastName.Trim()}".Trim();
        user.Name = fullName;
        user.AvatarUrl = AvatarHelper.GenerateUserAvatar(fullName);
        await _unitOfWork.Users.UpdateAsync(user);

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken);

        var (accessToken, refreshToken) = await CreateSessionAsync(user, userAgent);

        await LogAuditEvent(user.Id, AuditEventTypes.Login, ipAddress, userAgent);
        await _unitOfWork.SaveChangesAsync();

        return (user, accessToken, refreshToken);
    }

    public async Task ResendFirstAccessOtpAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user == null)
        {
            return;
        }

        var credentials = await _unitOfWork.UserCredentials.GetByUserIdAsync(user.Id);
        if (credentials != null && !string.IsNullOrWhiteSpace(credentials.PasswordHash))
        {
            return;
        }

        await SendOtpAsync(user);
    }

    private async Task<(string accessToken, string refreshToken)> CreateSessionAsync(User user, string userAgent)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _tokenHasher.HashToken(refreshTokenValue);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenLifetimeDays),
            DeviceInfo = userAgent
        };

        await _unitOfWork.RefreshTokens.CreateAsync(refreshToken);
        return (accessToken, refreshTokenValue);
    }

    private async Task SendOtpAsync(User user)
    {
        var otp = GenerateOtpCode();
        var tokenHash = _passwordResetOtpProtector.HashOtp(user.Id, otp);

        await _unitOfWork.PasswordResetTokens.InvalidateActiveTokensByUserIdAsync(user.Id);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AuthConstants.PasswordResetOtpLifetimeMinutes)
        };

        await _unitOfWork.PasswordResetTokens.CreateAsync(resetToken);

        var verificationModel = new VerificationCodeModel
        {
            Name = user.Name ?? "User",
            OTP = otp,
            ExpirationMinutes = AuthConstants.PasswordResetOtpLifetimeMinutes
        };
        _sendMailService.EnqueueVerificationCodeEmail(user.Email, verificationModel);

        await _unitOfWork.SaveChangesAsync();
    }

    private static string GenerateOtpCode()
    {
        var maxExclusive = (int)Math.Pow(10, AuthConstants.PasswordResetOtpLength);
        var value = RandomNumberGenerator.GetInt32(0, maxExclusive);
        return value.ToString($"D{AuthConstants.PasswordResetOtpLength}");
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "***";
        }

        var parts = email.Split('@', 2);
        var local = parts[0];
        var domain = parts[1];

        if (local.Length <= 2)
        {
            return $"**@{domain}";
        }

        return $"{local[0]}***{local[^1]}@{domain}";
    }

    private async Task LogAuditEvent(int userId, string eventType, string? ipAddress, string? userAgent)
    {
        if (userId <= 0)
        {
            return;
        }

        var auditEvent = new AuditEvent
        {
            UserId = userId,
            EventType = eventType,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _unitOfWork.AuditEvents.CreateAsync(auditEvent);
    }
}
