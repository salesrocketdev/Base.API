using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Base.Core.Tenant;
using Base.Domain.Entities;
using Base.Domain.Interfaces;

namespace Base.API.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public ExceptionLoggingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var userId = tenantContext.UserId > 0 ? tenantContext.UserId : (int?)null;
            var companyId = tenantContext.CompanyId > 0 ? tenantContext.CompanyId : (int?)null;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var log = new AppLog
                {
                    Level = LogLevel.Error,
                    Category = "UnhandledException",
                    Message = ex.Message,
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.ToString(),
                    UserId = userId,
                    CompanyId = companyId,
                    TraceId = context.TraceIdentifier,
                    CorrelationId = context.TraceIdentifier,
                    RequestPath = context.Request.Path.Value,
                    RequestMethod = context.Request.Method,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Source = ex.Source,
                    Details = BuildDetails(ex)
                };

                await unitOfWork.AppLogs.CreateAsync(log);
                await unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Avoid throwing during logging; we don't want to mask the original exception.
            }

            throw;
        }
    }

    private static string? BuildDetails(Exception ex)
    {
        if (ex.InnerException == null)
        {
            return null;
        }

        var details = new
        {
            innerExceptionType = ex.InnerException.GetType().FullName,
            innerExceptionMessage = ex.InnerException.Message
        };

        return JsonSerializer.Serialize(details);
    }
}

public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}


