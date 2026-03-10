namespace Base.Core.Security;

public interface IPasswordResetOtpProtector
{
    string HashOtp(int userId, string otp);
}
