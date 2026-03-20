using System.Security.Claims;
using Base.Domain.Interfaces;

namespace Base.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ??
            context.User.FindFirst("sub");

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            context.Items["UserId"] = userId;

            var tenantAccess = await unitOfWork.Users.GetTenantAccessByIdAsync(userId);
            if (tenantAccess != null)
            {
                context.Items["CompanyId"] = tenantAccess.CompanyId;
                context.Items["CompanyPublicId"] = tenantAccess.CompanyPublicId;

                if (!string.IsNullOrWhiteSpace(tenantAccess.UserRole))
                {
                    context.Items["UserRole"] = tenantAccess.UserRole;
                }
            }
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}


