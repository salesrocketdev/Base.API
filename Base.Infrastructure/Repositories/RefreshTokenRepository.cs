using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenAsync(string tokenHash)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == tokenHash && !rt.IsRevoked);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId)
    {
        return await _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow).ToListAsync();
    }

    public async Task RevokeAllTokensByUserIdAsync(int userId)
    {
        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }
}

