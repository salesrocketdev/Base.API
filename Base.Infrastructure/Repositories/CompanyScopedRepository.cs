using Microsoft.EntityFrameworkCore;
using Base.Core.Tenant;
using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;

namespace Base.Infrastructure.Repositories;

public class CompanyScopedRepository<TEntity> : TenantScopedRepository<TEntity> where TEntity : class
{
    public CompanyScopedRepository(ApplicationDbContext context, ITenantContext tenantContext)
        : base(context, tenantContext)
    {
    }

    protected override IQueryable<TEntity> ApplyTenantFilter(IQueryable<TEntity> query)
    {
        // For entities that belong to a company, filter by the current user's company
        if (_tenantContext.IsAuthenticated)
        {
            return base.ApplyTenantFilter(query);
        }

        // If not authenticated, return empty query (no data should be accessible)
        return query.Where(x => false);
    }
}

// Specific implementations for tenant-scoped entities
public class TenantScopedUserRepository : CompanyScopedRepository<User>, IUserRepository
{
    public TenantScopedUserRepository(ApplicationDbContext context, ITenantContext tenantContext)
        : base(context, tenantContext)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // For user lookup by email, we need to be careful about tenant isolation
        // In the MVP, users are unique across the system, but we should still check company ownership
        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user != null && _tenantContext.IsAuthenticated)
        {
            // Verify the user belongs to the current tenant's company
            if (user.CompanyId != _tenantContext.CompanyId)
            {
                return null; // User belongs to different company
            }
        }

        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        // For anonymous flows (signup/login), validate globally.
        // For authenticated flows, keep tenant scoping.
        if (!_tenantContext.IsAuthenticated)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        // Check if email exists within the current company
        if (_tenantContext.IsAuthenticated)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email && u.CompanyId == _tenantContext.CompanyId);
        }

        return false;
    }
}
