using AspNetCoreRateLimit;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Base.API.Extensions;
using Base.API.Middleware;
using Base.Core.Security;
using Base.Domain.Interfaces;
using Base.Infrastructure;
using Base.Infrastructure.Data;
using Base.Infrastructure.Seeding;
using Serilog;
using System.Linq;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var seqUrl = builder.Configuration["Logging:Seq:ServerUrl"];
var logLevel = builder.Configuration["Logging:Seq:MinimumLevel"];
var minimumLevel = Enum.TryParse<Serilog.Events.LogEventLevel>(logLevel, true, out var parsedLevel)
    ? parsedLevel
    : Serilog.Events.LogEventLevel.Information;

builder.Host.UseSerilog((context, logger) =>
{
    logger
        .MinimumLevel.Is(minimumLevel)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        logger.WriteTo.Seq(seqUrl);
    }
});

// Add services to the container.

// Database
builder.Services.AddApplicationDatabase(builder.Configuration);

// Security
builder.Services.AddApplicationSecurity(builder.Configuration);

// Services
builder.Services.AddApplicationServices();

// Email
builder.Services.AddApplicationEmail();

// Tenant Context
builder.Services.AddTenantContext();

// Hangfire
builder.Services.AddApplicationHangfire(builder.Configuration);

// Authentication
builder.Services.AddApplicationAuthentication(builder.Configuration);

// Rate Limiting
builder.Services.AddApplicationRateLimiting(builder.Configuration);

// CORS
builder.Services.AddApplicationCors(builder.Configuration);

// Controllers
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

// OpenAPI (built-in)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

// Rate limiting
app.UseIpRateLimiting();

// CORS
app.UseCors("AllowBFF");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Tenant Middleware
app.UseTenantMiddleware();

app.UseExceptionLogging();

// Hangfire Dashboard
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Hangfire:Enabled", true) && app.Configuration.GetValue("Hangfire:UseDashboard", false))
{
    app.UseHangfireDashboard("/hangfire");
}

// Controllers
app.MapControllers();

// Health
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTimeOffset.UtcNow
}));

var isEfTool = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => string.Equals(a.GetName().Name, "Microsoft.EntityFrameworkCore.Design", StringComparison.Ordinal));
var isEfEnv = string.Equals(Environment.GetEnvironmentVariable("DOTNET_EF"), "1", StringComparison.Ordinal)
    || string.Equals(Environment.GetEnvironmentVariable("EFCORE_DESIGNTIME"), "1", StringComparison.Ordinal);

if (!isEfTool && !isEfEnv)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seederRunner = scope.ServiceProvider.GetRequiredService<SeederRunner>();
    await seederRunner.RunAsync();
}

app.Run();


