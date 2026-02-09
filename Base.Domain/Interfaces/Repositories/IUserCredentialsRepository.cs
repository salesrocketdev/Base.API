using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IUserCredentialsRepository : IBaseRepository<UserCredentials>
{
    Task<UserCredentials?> GetByUserIdAsync(int userId);
}
