using Base.Core.Tenant;

namespace Base.Domain.Interfaces.Repositories;

public interface ITenantScopedRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    // Additional methods for tenant-scoped operations can be added here
}

