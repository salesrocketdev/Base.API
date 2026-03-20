using Base.Domain.Entities;

namespace Base.Domain.Interfaces.Repositories;

public interface IAuditEventRepository : IBaseRepository<AuditEvent>
{
    Task<IReadOnlyList<AuditEvent>> GetByUserIdAsync(int userId, int take = 100);
}

