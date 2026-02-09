namespace Base.Domain.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<TEntity?> GetByPublicIdAsync(Guid publicId);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

