using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class AppLogRepository : BaseRepository<AppLog>, IAppLogRepository
{
    public AppLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AppLog>> GetRecentAsync(int take = 100, int? companyId = null)
    {
        take = Math.Clamp(take, 1, 500);

        var query = _context.AppLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(log => log.CompanyId == companyId.Value);
        }

        return await query
            .Take(take)
            .ToListAsync();
    }
}



