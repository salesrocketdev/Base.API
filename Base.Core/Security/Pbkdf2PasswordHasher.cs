using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Base.Core.Security;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int DefaultIterations = 600000; // Adjust via configuration per environment
    private readonly int _iterations;

    public Pbkdf2PasswordHasher(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["Security:PasswordHashing:Iterations"], out _iterations) || _iterations <= 0)
        {
            _iterations = DefaultIterations;
        }
    }

    public string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, KeySize);

        var hashBytes = new byte[SaltSize + KeySize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string hashedPassword, string password)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);

        if (hashBytes.Length != SaltSize + KeySize)
        {
            return false;
        }

        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, KeySize);
        return CryptographicOperations.FixedTimeEquals(
            hashBytes.AsSpan(SaltSize, KeySize),
            hash
        );
    }
}

