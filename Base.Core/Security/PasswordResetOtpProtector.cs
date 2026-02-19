using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Base.Core.Security;

public class PasswordResetOtpProtector : IPasswordResetOtpProtector
{
    private readonly byte[] _pepperKey;

    public PasswordResetOtpProtector(IConfiguration configuration)
    {
        var pepper = configuration["Security:PasswordReset:Pepper"];
        if (string.IsNullOrWhiteSpace(pepper))
        {
            pepper = configuration["Jwt:SecretKey"];
        }

        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException("Password reset pepper is required. Configure Security:PasswordReset:Pepper or Jwt:SecretKey.");
        }

        _pepperKey = Encoding.UTF8.GetBytes(pepper);
    }

    public string HashOtp(int userId, string otp)
    {
        var payload = Encoding.UTF8.GetBytes($"{userId}:{otp}");
        using var hmac = new HMACSHA256(_pepperKey);
        var hash = hmac.ComputeHash(payload);
        return Convert.ToBase64String(hash);
    }
}
