namespace Base.Domain.Models;

public sealed record UserTenantAccess(
    int UserId,
    int CompanyId,
    Guid CompanyPublicId,
    string UserRole);
