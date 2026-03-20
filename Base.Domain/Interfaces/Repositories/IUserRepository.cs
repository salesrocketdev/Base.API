using Base.Domain.Entities;
using Base.Domain.Models;

namespace Base.Domain.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<UserTenantAccess?> GetTenantAccessByIdAsync(int userId);
}

