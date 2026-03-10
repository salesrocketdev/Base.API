using Base.Domain.Entities;

namespace Base.Domain.Interfaces.Repositories;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByUserAndTokenHashAsync(int userId, string tokenHash);
}

