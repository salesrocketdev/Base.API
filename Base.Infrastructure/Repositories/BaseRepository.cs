using Base.Domain.Interfaces;
using Base.Domain.Entities;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly ApplicationDbContext _context;

    public BaseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    public async Task<TEntity?> GetByPublicIdAsync(Guid publicId)
    {
        var entity = await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "PublicId") == publicId);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _context.Set<TEntity>().ToListAsync();
    }

    public Task<TEntity> CreateAsync(TEntity entity)
    {
        _context.Set<TEntity>().Add(entity);
        return Task.FromResult(entity);
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
        return Task.FromResult(entity);
    }

    public async Task DeleteAsync(int id)
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
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Set<TEntity>().FindAsync(id) != null;
    }
}




