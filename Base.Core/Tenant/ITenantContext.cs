namespace Base.Core.Tenant;

public interface ITenantContext
{
    int CompanyId { get; }
    Guid CompanyPublicId { get; }
    int UserId { get; }
    string UserRole { get; }
    bool IsAuthenticated { get; }
}

