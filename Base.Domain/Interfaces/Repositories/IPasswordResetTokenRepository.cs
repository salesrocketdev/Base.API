using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByUserIdAndTokenHashAsync(int userId, string tokenHash);
    Task InvalidateActiveTokensByUserIdAsync(int userId);
}
