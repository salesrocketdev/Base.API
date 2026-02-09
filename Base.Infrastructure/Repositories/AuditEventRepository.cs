using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class AuditEventRepository : BaseRepository<AuditEvent>, IAuditEventRepository
{
    public AuditEventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditEvent>> GetByUserIdAsync(int userId)
    {
        return await _context.AuditEvents.Where(a => a.UserId == userId).OrderByDescending(a => a.Timestamp).ToListAsync();
    }
}
