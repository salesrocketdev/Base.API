using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IAuditEventRepository : IBaseRepository<AuditEvent>
{
    Task<IEnumerable<AuditEvent>> GetByUserIdAsync(int userId);
}
