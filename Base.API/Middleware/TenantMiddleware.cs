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
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user != null)
            {
                context.Items["UserId"] = userId;

                var membership = await unitOfWork.CompanyMembers.GetByUserIdAsync(userId);
                var companyId = user.CompanyId > 0
                    ? user.CompanyId
                    : membership?.CompanyId ?? 0;

                if (companyId > 0)
                {
                    context.Items["CompanyId"] = companyId;

                    var company = await unitOfWork.Companies.GetByIdAsync(companyId);
                    if (company != null)
                    {
                        context.Items["CompanyPublicId"] = company.PublicId;
                    }
                }

                if (membership != null && membership.CompanyId == companyId)
                {
                    context.Items["UserRole"] = membership.Role;
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


