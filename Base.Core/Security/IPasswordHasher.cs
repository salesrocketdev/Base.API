namespace Base.Core.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    PasswordVerificationResult VerifyPassword(string hashedPassword, string password);
}
