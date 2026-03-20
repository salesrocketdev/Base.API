using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByUserIdAndTokenHashAsync(int userId, string tokenHash)
    {
        return await _context.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(prt =>
                prt.UserId == userId &&
                prt.TokenHash == tokenHash &&
                !prt.IsUsed &&
                prt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task InvalidateActiveTokensByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;

        await _context.PasswordResetTokens
            .Where(prt => prt.UserId == userId && !prt.IsUsed && prt.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(prt => prt.IsUsed, true)
                .SetProperty(prt => prt.UsedAt, now));

        foreach (var entry in _context.ChangeTracker.Entries<PasswordResetToken>()
                     .Where(entry => entry.Entity.UserId == userId && !entry.Entity.IsUsed && entry.Entity.ExpiresAt > now))
        {
            entry.Entity.IsUsed = true;
            entry.Entity.UsedAt = now;
        }
    }
}
