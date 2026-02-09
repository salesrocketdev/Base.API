using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.PasswordResetTokens.FirstOrDefaultAsync(prt => prt.TokenHash == tokenHash && !prt.IsUsed && prt.ExpiresAt > DateTime.UtcNow);
    }
}
