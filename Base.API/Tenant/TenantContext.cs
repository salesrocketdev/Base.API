using Microsoft.AspNetCore.Http;
using Base.Core.Tenant;

namespace Base.API.Tenant;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int CompanyId => GetCompanyId();
    public Guid CompanyPublicId => GetCompanyPublicId();
    public int UserId => GetUserId();
    public string UserRole => GetUserRole();
    public bool IsAuthenticated => CompanyId > 0;

    private int GetCompanyId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Items["CompanyId"] as int? ?? 0;
    }

    private Guid GetCompanyPublicId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Items["CompanyPublicId"] as Guid? ?? Guid.Empty;
    }

    private int GetUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Items["UserId"] as int? ?? 0;
    }

    private string GetUserRole()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Items["UserRole"] as string ?? string.Empty;
    }
}


