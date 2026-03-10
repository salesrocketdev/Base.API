namespace Base.Core.Security;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(int userId, string email, Guid? organizationPublicId = null);
    string GenerateRefreshToken();
}
