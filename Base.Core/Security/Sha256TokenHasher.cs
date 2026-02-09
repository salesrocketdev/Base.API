using System.Security.Cryptography;
using System.Text;

namespace Base.Core.Security;

public class Sha256TokenHasher : ITokenHasher
{
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
