using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Base.API.Middleware;
using Base.Domain.Entities;
using Base.Infrastructure;
using Base.Infrastructure.Data;

namespace Base.Tests;

public class TenantMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PopulatesTenantContext_FromUserCompanyMembership()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var company = new Company { Name = "tenant-company" };
        await db.Companies.AddAsync(company);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email = "tenant@test.local",
            CompanyId = company.Id
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        await db.CompanyMembers.AddAsync(new CompanyMember
        {
            CompanyId = company.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("sub", user.Id.ToString())
            },
            "test"));

        var middleware = new TenantMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, new UnitOfWork(db));

        Assert.Equal(user.Id, Assert.IsType<int>(context.Items["UserId"]));
        Assert.Equal(company.Id, Assert.IsType<int>(context.Items["CompanyId"]));
        Assert.Equal(company.PublicId, Assert.IsType<Guid>(context.Items["CompanyPublicId"]));
        Assert.Equal("Owner", Assert.IsType<string>(context.Items["UserRole"]));
    }
}
