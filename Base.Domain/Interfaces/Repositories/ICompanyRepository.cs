using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface ICompanyRepository : IBaseRepository<Company>
{
    Task<Company?> GetByIdWithMembersAsync(int id);
    Task<Company?> GetByPublicIdWithMembersAsync(Guid publicId);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}

public interface ICompanyMemberRepository : IBaseRepository<CompanyMember>
{
    Task<CompanyMember?> GetByUserIdAsync(int userId);
    Task<CompanyMember?> GetByCompanyAndUserIdAsync(int companyId, int userId);
    Task<bool> IsUserInAnyCompanyAsync(int userId);
}

