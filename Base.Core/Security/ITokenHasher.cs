namespace Base.Core.Security;

public interface ITokenHasher
{
    string HashToken(string token);
}
