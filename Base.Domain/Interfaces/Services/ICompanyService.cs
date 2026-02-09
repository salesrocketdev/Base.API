using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface ICompanyService
{
    Task<Company> CreateAsync(string name, string? settingsJson = null);
    Task<Company?> GetByIdAsync(Guid publicId);
    Task<Company?> GetByIdWithMembersAsync(Guid publicId);
    Task UpdateAsync(Guid publicId, string name, string? settingsJson = null);
    Task DeleteAsync(Guid publicId);

    // Helpers for current tenant
    Task<Company?> GetByCompanyIdWithMembersAsync(int companyId);
    Task UpdateByCompanyIdAsync(int companyId, string name, string? settingsJson = null);
    Task<CompanyMember> InviteMemberAsync(int companyId, string email, string role);
}


