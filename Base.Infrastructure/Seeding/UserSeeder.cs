using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Base.Core.Helpers;
using Base.Core.Security;
using Base.Domain.Entities;
using Base.Infrastructure.Data;

namespace Base.Infrastructure.Seeding;

public class UserSeeder : ISeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public UserSeeder(ApplicationDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public int Order => 2;

    public async Task SeedAsync()
    {
        var enabled = _configuration.GetValue("Seed:Enabled", false);
        if (!enabled)
        {
            return;
        }

        if (await _db.Users.AnyAsync())
        {
            return;
        }

        var email = _configuration["Seed:AdminEmail"];
        var password = _configuration["Seed:AdminPassword"];
        var name = _configuration["Seed:AdminName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var company = await _db.Companies.FirstOrDefaultAsync();
        if (company == null)
        {
            return;
        }

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            Name = string.IsNullOrWhiteSpace(name) ? null : name,
            IsActive = true,
            AvatarUrl = AvatarHelper.GenerateUserRoleAvatar(name ?? "Admin", "admin"),
            CompanyId = company.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var credentials = new UserCredentials
        {
            UserId = user.Id,
            PasswordHash = _passwordHasher.HashPassword(password)
        };

        _db.UserCredentials.Add(credentials);
        await _db.SaveChangesAsync();

        var companyMember = new CompanyMember
        {
            CompanyId = company.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow
        };

        _db.CompanyMembers.Add(companyMember);
        await _db.SaveChangesAsync();
    }
}

