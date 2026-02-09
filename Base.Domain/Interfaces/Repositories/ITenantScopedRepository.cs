using Base.Core.Tenant;

namespace Base.Domain.Interfaces;

public interface ITenantScopedRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    // Additional methods for tenant-scoped operations can be added here
}
