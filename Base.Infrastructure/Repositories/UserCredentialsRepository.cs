using Microsoft.EntityFrameworkCore;
using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;

namespace Base.Infrastructure.Repositories;

public class UserCredentialsRepository : BaseRepository<UserCredentials>, IUserCredentialsRepository
{
    public UserCredentialsRepository(ApplicationDbContext context) : base(context) { }

    public async Task<UserCredentials?> GetByUserIdAsync(int userId)
    {
        return await _context.UserCredentials.FirstOrDefaultAsync(uc => uc.UserId == userId);
    }
}
