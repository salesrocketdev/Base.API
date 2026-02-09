using Microsoft.EntityFrameworkCore;
using Base.Core.Tenant;
using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;

namespace Base.Infrastructure.Repositories;

public abstract class TenantScopedRepository<TEntity> : ITenantScopedRepository<TEntity> where TEntity : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly ITenantContext _tenantContext;

    protected TenantScopedRepository(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id)
    {
        // Apply tenant filtering if entity has CompanyId property
        var entity = await _context.Set<TEntity>().FindAsync(id);

        if (entity != null && HasCompanyIdProperty(entity) && !IsEntityOwnedByCurrentTenant(entity))
        {
            return null; // Entity belongs to different tenant
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetByPublicIdAsync(Guid publicId)
    {
        var entity = await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "PublicId") == publicId);

        if (entity != null && HasCompanyIdProperty(entity) && !IsEntityOwnedByCurrentTenant(entity))
        {
            return null; // Entity belongs to different tenant
        }

        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        var query = _context.Set<TEntity>().AsQueryable();

        // Apply tenant filtering if entity has CompanyId property
        if (HasCompanyIdProperty(typeof(TEntity)))
        {
            query = ApplyTenantFilter(query);
        }

        return await query.ToListAsync();
    }

    public virtual Task<TEntity> CreateAsync(TEntity entity)
    {
        // Set CompanyId if entity has the property and user is authenticated
        if (_tenantContext.IsAuthenticated && HasCompanyIdProperty(entity))
        {
            SetCompanyId(entity, _tenantContext.CompanyId);
        }

        _context.Set<TEntity>().Add(entity);
        return Task.FromResult(entity);
    }

    public virtual Task<TEntity> UpdateAsync(TEntity entity)
    {
        // Verify ownership before update
        if (HasCompanyIdProperty(entity) && !IsEntityOwnedByCurrentTenant(entity))
        {
            throw new UnauthorizedAccessException("Cannot modify entity from different tenant");
        }

        _context.Set<TEntity>().Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.DeletedAt = DateTime.UtcNow;
                baseEntity.UpdatedAt = DateTime.UtcNow;
                _context.Set<TEntity>().Update(entity);
            }
            else
            {
                _context.Set<TEntity>().Remove(entity);
            }
        }
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        return entity != null;
    }

    protected virtual IQueryable<TEntity> ApplyTenantFilter(IQueryable<TEntity> query)
    {
        // This method should be overridden in derived classes to apply specific tenant filtering
        // For entities with CompanyId property, filter by current tenant's company
        if (_tenantContext.IsAuthenticated)
        {
            // Use reflection to build the filter expression
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, "CompanyId");
            var constant = System.Linq.Expressions.Expression.Constant(_tenantContext.CompanyId);
            var equality = System.Linq.Expressions.Expression.Equal(property, constant);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(equality, parameter);

            query = query.Where(lambda);
        }

        return query;
    }

    private bool HasCompanyIdProperty(Type type)
    {
        return type.GetProperty("CompanyId") != null;
    }

    private bool HasCompanyIdProperty(object entity)
    {
        return HasCompanyIdProperty(entity.GetType());
    }

    private bool IsEntityOwnedByCurrentTenant(object entity)
    {
        if (!_tenantContext.IsAuthenticated)
            return false;

        var companyIdProperty = entity.GetType().GetProperty("CompanyId");
        if (companyIdProperty == null)
            return true; // No CompanyId property means no tenant isolation needed

        var entityCompanyId = (int?)companyIdProperty.GetValue(entity);
        return entityCompanyId == _tenantContext.CompanyId;
    }

    private void SetCompanyId(object entity, int companyId)
    {
        var companyIdProperty = entity.GetType().GetProperty("CompanyId");
        if (companyIdProperty != null && companyIdProperty.CanWrite)
        {
            companyIdProperty.SetValue(entity, companyId);
        }
    }
}







