using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash && !rt.IsRevoked);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task RevokeAllTokensByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;

        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, now));

        foreach (var entry in _context.ChangeTracker.Entries<RefreshToken>()
                     .Where(entry => entry.Entity.UserId == userId && !entry.Entity.IsRevoked))
        {
            entry.Entity.IsRevoked = true;
            entry.Entity.RevokedAt = now;
        }
    }
}


