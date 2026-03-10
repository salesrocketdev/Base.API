using Base.Domain.Entities;

namespace Base.Domain.Interfaces.Repositories;

public interface IAuditEventRepository : IBaseRepository<AuditEvent>
{
    Task<IEnumerable<AuditEvent>> GetByUserIdAsync(int userId);
}

