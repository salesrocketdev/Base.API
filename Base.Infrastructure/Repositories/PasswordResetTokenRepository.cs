using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByUserIdAndTokenHashAsync(int userId, string tokenHash)
    {
        return await _context.PasswordResetTokens
            .Include(prt => prt.User)
            .FirstOrDefaultAsync(prt =>
                prt.UserId == userId &&
                prt.TokenHash == tokenHash &&
                !prt.IsUsed &&
                prt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task InvalidateActiveTokensByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await _context.PasswordResetTokens
            .Where(prt => prt.UserId == userId && !prt.IsUsed && prt.ExpiresAt > now)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsUsed = true;
            token.UsedAt = now;
        }
    }
}
