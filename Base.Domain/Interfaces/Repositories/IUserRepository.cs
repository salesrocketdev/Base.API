using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
}
