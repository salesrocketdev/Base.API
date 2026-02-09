namespace Base.Core.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string password);
}
