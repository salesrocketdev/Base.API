using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
}
