using System.Text.Json;
using Base.Domain.Entities;
using Base.Domain.Interfaces;

namespace Base.Domain.Services;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Company> CreateAsync(string name, string? settingsJson = null)
    {
        if (await _unitOfWork.Companies.NameExistsAsync(name))
        {
            throw new InvalidOperationException("Company name already exists.");
        }

        var company = new Company
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(settingsJson))
        {
            company.Settings = JsonDocument.Parse(settingsJson);
        }

        await _unitOfWork.Companies.CreateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        return company;
    }

    public async Task<Company?> GetByIdAsync(Guid publicId)
    {
        return await _unitOfWork.Companies.GetByPublicIdAsync(publicId);
    }

    public async Task<Company?> GetByIdWithMembersAsync(Guid publicId)
    {
        return await _unitOfWork.Companies.GetByPublicIdWithMembersAsync(publicId);
    }

    public async Task UpdateAsync(Guid publicId, string name, string? settingsJson = null)
    {
        var company = await _unitOfWork.Companies.GetByPublicIdAsync(publicId);
        if (company == null) throw new InvalidOperationException("Company not found.");

        if (await _unitOfWork.Companies.NameExistsAsync(name, company.Id))
        {
            throw new InvalidOperationException("Company name already exists.");
        }

        company.Name = name;
        company.UpdatedAt = DateTime.UtcNow;

        if (settingsJson != null)
        {
            company.Settings = JsonDocument.Parse(settingsJson);
        }

        await _unitOfWork.Companies.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid publicId)
    {
        var company = await _unitOfWork.Companies.GetByPublicIdAsync(publicId);
        if (company == null)
        {
            throw new InvalidOperationException("Company not found.");
        }

        await _unitOfWork.Companies.DeleteAsync(company.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    // --- Tenant-scoped helpers ---
    public async Task<Company?> GetByCompanyIdWithMembersAsync(int companyId)
    {
        return await _unitOfWork.Companies.GetByIdWithMembersAsync(companyId);
    }

    public async Task UpdateByCompanyIdAsync(int companyId, string name, string? settingsJson = null)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
        if (company == null) throw new InvalidOperationException("Company not found.");

        if (await _unitOfWork.Companies.NameExistsAsync(name, companyId))
        {
            throw new InvalidOperationException("Company name already exists.");
        }

        company.Name = name;
        company.UpdatedAt = DateTime.UtcNow;

        if (settingsJson != null)
        {
            company.Settings = JsonDocument.Parse(settingsJson);
        }

        await _unitOfWork.Companies.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CompanyMember> InviteMemberAsync(int companyId, string email, string role)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email.ToLower());
        if (user == null)
        {
            throw new InvalidOperationException("User not found. Invitation flow for unknown users is not implemented.");
        }

        // User must not be already in any company (MVP constraint: 1 user ↔ 1 company)
        if (await _unitOfWork.CompanyMembers.IsUserInAnyCompanyAsync(user.Id))
        {
            throw new InvalidOperationException("User is already a member of a company.");
        }

                var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
        if (company == null)
        {
            throw new InvalidOperationException("Company not found.");
        }

        var companyMember = new CompanyMember
        {
            CompanyId = companyId,
            UserId = user.Id,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            Company = company,
            User = user
        };

        var created = await _unitOfWork.CompanyMembers.CreateAsync(companyMember);

        // Update user's CompanyId for convenience (MVP behavior)
        user.CompanyId = companyId;
        await _unitOfWork.Users.UpdateAsync(user);

        await _unitOfWork.SaveChangesAsync();

        return created;
    }
}



