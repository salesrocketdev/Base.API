using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class AuditEventRepository : BaseRepository<AuditEvent>, IAuditEventRepository
{
    public AuditEventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AuditEvent>> GetByUserIdAsync(int userId, int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        return await _context.AuditEvents
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
    }
}

