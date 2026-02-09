using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string tokenHash);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId);
    Task RevokeAllTokensByUserIdAsync(int userId);
}
