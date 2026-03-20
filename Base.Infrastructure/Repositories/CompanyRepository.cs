using Microsoft.EntityFrameworkCore;
using Base.Domain.Entities;
using Base.Domain.Interfaces.Repositories;
using Base.Infrastructure.Data;

namespace Base.Infrastructure.Repositories;

public class CompanyRepository : BaseRepository<Company>, ICompanyRepository
{
    public CompanyRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Company?> GetByIdWithMembersAsync(int id)
    {
        return await _context.Companies
            .AsNoTracking()
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByPublicIdWithMembersAsync(Guid publicId)
    {
        return await _context.Companies
            .AsNoTracking()
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .FirstOrDefaultAsync(c => c.PublicId == publicId);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        var normalizedName = ApplicationDbContext.NormalizeCompanyName(name);

        var query = _context.Companies
            .Where(c => EF.Property<string>(c, "NameNormalized") == normalizedName);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}

public class CompanyMemberRepository : BaseRepository<CompanyMember>, ICompanyMemberRepository
{
    public CompanyMemberRepository(ApplicationDbContext context) : base(context) { }

    public async Task<CompanyMember?> GetByUserIdAsync(int userId)
    {
        return await _context.CompanyMembers
            .AsNoTracking()
            .Include(cm => cm.Company)
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.UserId == userId);
    }

    public async Task<CompanyMember?> GetByCompanyAndUserIdAsync(int companyId, int userId)
    {
        return await _context.CompanyMembers
            .AsNoTracking()
            .Include(cm => cm.Company)
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.UserId == userId);
    }

    public async Task<bool> IsUserInAnyCompanyAsync(int userId)
    {
        return await _context.CompanyMembers.AnyAsync(cm => cm.UserId == userId);
    }
}



