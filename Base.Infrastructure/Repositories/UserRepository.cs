using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Domain.Models;
using Base.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Base.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<UserTenantAccess?> GetTenantAccessByIdAsync(int userId)
    {
        return await (
            from user in _context.Users.AsNoTracking()
            join membership in _context.CompanyMembers.AsNoTracking()
                on new { UserId = user.Id, user.CompanyId } equals new { membership.UserId, membership.CompanyId }
            join company in _context.Companies.AsNoTracking()
                on user.CompanyId equals company.Id
            where user.Id == userId && user.CompanyId > 0
            select new UserTenantAccess(
                user.Id,
                user.CompanyId,
                company.PublicId,
                membership.Role))
            .FirstOrDefaultAsync();
    }
}

