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
        // Extract user ID from JWT token
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ??
                        context.User.FindFirst("sub");

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            // Get company member to resolve company ID
            var companyMember = await unitOfWork.CompanyMembers.GetByUserIdAsync(userId);
            if (companyMember != null)
            {
                // Add company ID to HttpContext.Items for use in services/repositories
                context.Items["CompanyId"] = companyMember.CompanyId;
                context.Items["UserId"] = userId;

                var company = await unitOfWork.Companies.GetByIdAsync(companyMember.CompanyId);
                if (company != null)
                {
                    context.Items["CompanyPublicId"] = company.PublicId;
                }
                context.Items["UserRole"] = companyMember.Role;
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


