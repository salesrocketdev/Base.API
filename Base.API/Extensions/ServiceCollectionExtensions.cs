using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Base.API.Tenant;
using Base.Core.Email;
using Base.Core.Security;
using Base.Core.Tenant;
using Base.Domain.Interfaces;
using Base.Domain.Services;
using Base.Infrastructure;
using Base.Infrastructure.Data;
using Base.Infrastructure.Seeding;
using System.Text;

namespace Base.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   // We run migrations via the migrator container. Avoid crashing the API on model drift.
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        return services;
    }

    public static IServiceCollection AddApplicationSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenHasher, Sha256TokenHasher>();
        services.AddScoped<IPasswordResetOtpProtector, PasswordResetOtpProtector>();
        services.AddScoped<IJwtTokenGenerator>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new JwtTokenGenerator(
                config["Jwt:SecretKey"]!,
                config["Jwt:Issuer"]!,
                config["Jwt:Audience"]!,
                int.Parse(config["Jwt:AccessTokenExpirationMinutes"]!),
                int.Parse(config["Jwt:RefreshTokenExpirationDays"]!)
            );
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<SeederRunner>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, CompanySeeder>();

        return services;
    }

    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!))
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

        return services;
    }

    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowBFF", policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                    return;
                }

                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IServiceCollection AddApplicationHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireEnabled = configuration.GetValue("Hangfire:Enabled", true);
        if (!hangfireEnabled)
        {
            return services;
        }

        var storage = configuration["Hangfire:Storage"] ?? "PostgreSql";
        var hangfireConnection = configuration.GetConnectionString("HangfireConnection")
            ?? configuration["Hangfire:ConnectionString"]
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (string.Equals(storage, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                config.UseInMemoryStorage();
                return;
            }

            if (string.IsNullOrWhiteSpace(hangfireConnection))
            {
                throw new InvalidOperationException("Hangfire PostgreSQL storage requires Hangfire:ConnectionString or ConnectionStrings:DefaultConnection.");
            }

            config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection));
        });

        services.AddHangfireServer();

        return services;
    }

    public static IServiceCollection AddApplicationEmail(this IServiceCollection services)
    {
        services.AddScoped<ISendMailService, SendMailService>();

        return services;
    }

    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        return services;
    }
}

