using Base.Domain.Entities;

namespace Base.Domain.Interfaces.Repositories;

public interface IAppLogRepository : IBaseRepository<AppLog>
{
    Task<IReadOnlyList<AppLog>> GetRecentAsync(int take = 100, int? companyId = null);
}



