using Base.Domain.Entities;

namespace Base.Domain.Interfaces.Repositories;

public interface IUserCredentialsRepository : IBaseRepository<UserCredentials>
{
    Task<UserCredentials?> GetByUserIdAsync(int userId);
}

