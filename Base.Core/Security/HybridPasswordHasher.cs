namespace Base.Core.Security;

public class HybridPasswordHasher : IPasswordHasher
{
    private readonly Argon2idPasswordHasher _argon2idPasswordHasher;
    private readonly Pbkdf2PasswordHasher _pbkdf2PasswordHasher;

    public HybridPasswordHasher(Argon2idPasswordHasher argon2idPasswordHasher, Pbkdf2PasswordHasher pbkdf2PasswordHasher)
    {
        _argon2idPasswordHasher = argon2idPasswordHasher;
        _pbkdf2PasswordHasher = pbkdf2PasswordHasher;
    }

    public string HashPassword(string password)
    {
        return _argon2idPasswordHasher.HashPassword(password);
    }

    public PasswordVerificationResult VerifyPassword(string hashedPassword, string password)
    {
        var argon2idResult = _argon2idPasswordHasher.VerifyPassword(hashedPassword, password);
        if (argon2idResult.Succeeded)
        {
            return argon2idResult;
        }

        return _pbkdf2PasswordHasher.VerifyPassword(hashedPassword, password);
    }
}
