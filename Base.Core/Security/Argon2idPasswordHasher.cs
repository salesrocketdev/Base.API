using Microsoft.Extensions.Configuration;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using System.Text;

namespace Base.Core.Security;

public class Argon2idPasswordHasher
{
    private const int DefaultTimeCost = 3;
    private const int DefaultMemoryCost = 65536;
    private const int DefaultLanes = 1;
    private const int DefaultThreads = 1;
    private const int DefaultHashLength = 32;
    private const int DefaultSaltLength = 16;
    private const string Argon2IdPrefix = "$argon2id$";

    private readonly int _timeCost;
    private readonly int _memoryCost;
    private readonly int _lanes;
    private readonly int _threads;
    private readonly int _hashLength;
    private readonly int _saltLength;
    private readonly string _pepper;

    public Argon2idPasswordHasher(IConfiguration configuration)
    {
        _timeCost = ReadPositiveInt(configuration["Security:PasswordHashing:TimeCost"], DefaultTimeCost);
        _memoryCost = ReadPositiveInt(configuration["Security:PasswordHashing:MemoryCost"], DefaultMemoryCost);
        _lanes = ReadPositiveInt(configuration["Security:PasswordHashing:Lanes"], DefaultLanes);
        _threads = ReadPositiveInt(configuration["Security:PasswordHashing:Threads"], DefaultThreads);
        _hashLength = ReadPositiveInt(configuration["Security:PasswordHashing:HashLength"], DefaultHashLength);
        _saltLength = ReadPositiveInt(configuration["Security:PasswordHashing:SaltLength"], DefaultSaltLength);
        _pepper = configuration["Security:PasswordHashing:Pepper"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_pepper))
        {
            throw new InvalidOperationException("Password hashing pepper is required. Configure Security:PasswordHashing:Pepper.");
        }
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_saltLength);
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = _timeCost,
            MemoryCost = _memoryCost,
            Lanes = _lanes,
            Threads = _threads,
            HashLength = _hashLength,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = salt,
            Secret = Encoding.UTF8.GetBytes(_pepper),
            ClearPassword = true,
            ClearSecret = true
        };

        return Argon2.Hash(config);
    }

    public PasswordVerificationResult VerifyPassword(string hashedPassword, string password)
    {
        if (!IsArgon2idHash(hashedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        try
        {
            var succeeded = Argon2.Verify(
                hashedPassword,
                Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(_pepper),
                _threads);

            if (!succeeded)
            {
                return PasswordVerificationResult.Failed;
            }

            return NeedsRehash(hashedPassword)
                ? PasswordVerificationResult.SuccessRehashRequired
                : PasswordVerificationResult.Success;
        }
        catch (Exception) when (hashedPassword.StartsWith(Argon2IdPrefix, StringComparison.Ordinal))
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private bool NeedsRehash(string hashedPassword)
    {
        if (!TryReadParameters(hashedPassword, out var timeCost, out var memoryCost, out var lanes, out var saltLength, out var hashLength))
        {
            return true;
        }

        return timeCost != _timeCost
            || memoryCost != _memoryCost
            || lanes != _lanes
            || saltLength != _saltLength
            || hashLength != _hashLength;
    }

    private static bool IsArgon2idHash(string hashedPassword)
    {
        return hashedPassword.StartsWith(Argon2IdPrefix, StringComparison.Ordinal);
    }

    private static bool TryReadParameters(string hashedPassword, out int timeCost, out int memoryCost, out int lanes, out int saltLength, out int hashLength)
    {
        timeCost = 0;
        memoryCost = 0;
        lanes = 0;
        saltLength = 0;
        hashLength = 0;

        var segments = hashedPassword.Split('$', StringSplitOptions.None);
        if (segments.Length < 6 || !string.Equals(segments[1], "argon2id", StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryReadArgon2Metadata(segments[2], segments[3], out timeCost, out memoryCost, out lanes))
        {
            return false;
        }

        try
        {
            saltLength = Convert.FromBase64String(PadBase64(segments[4])).Length;
            hashLength = Convert.FromBase64String(PadBase64(segments[5])).Length;
            return saltLength > 0 && hashLength > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryReadArgon2Metadata(string versionSegment, string parameterSegment, out int timeCost, out int memoryCost, out int lanes)
    {
        timeCost = 0;
        memoryCost = 0;
        lanes = 0;

        if (!string.Equals(versionSegment, "v=19", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var parameter in parameterSegment.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length != 2 || !int.TryParse(parts[1], out var value) || value <= 0)
            {
                return false;
            }

            switch (parts[0])
            {
                case "m":
                    memoryCost = value;
                    break;
                case "t":
                    timeCost = value;
                    break;
                case "p":
                    lanes = value;
                    break;
            }
        }

        return timeCost > 0 && memoryCost > 0 && lanes > 0;
    }

    private static string PadBase64(string value)
    {
        return value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
    }

    private static int ReadPositiveInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}
