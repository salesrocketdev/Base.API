using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByUserAndTokenHashAsync(int userId, string tokenHash)
    {
        return await _context.PasswordResetTokens.FirstOrDefaultAsync(prt =>
            prt.UserId == userId &&
            prt.TokenHash == tokenHash &&
            !prt.IsUsed &&
            prt.ExpiresAt > DateTime.UtcNow);
    }
}

