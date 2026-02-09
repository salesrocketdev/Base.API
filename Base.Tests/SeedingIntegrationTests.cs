using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Base.Core.Security;
using Base.Infrastructure.Data;
using Base.Infrastructure.Seeding;

namespace Base.Tests;

public class SeedingIntegrationTests
{
    [Fact]
    public async Task CompanyAndUserSeeders_AreIdempotent()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Enabled"] = "true",
                ["Seed:CompanyName"] = "Base Demo",
                ["Seed:AdminEmail"] = "admin@base.local",
                ["Seed:AdminPassword"] = "StrongPassword!123",
                ["Seed:AdminName"] = "Administrator",
                ["Security:PasswordHashing:Iterations"] = "1000"
            })
            .Build();

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hasher = new Pbkdf2PasswordHasher(configuration);
        var companySeeder = new CompanySeeder(db, configuration, NullLogger<CompanySeeder>.Instance);
        var userSeeder = new UserSeeder(db, hasher, configuration);

        await companySeeder.SeedAsync();
        await userSeeder.SeedAsync();

        await companySeeder.SeedAsync();
        await userSeeder.SeedAsync();

        Assert.Equal(1, await db.Companies.CountAsync());
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(1, await db.UserCredentials.CountAsync());
        Assert.Equal(1, await db.CompanyMembers.CountAsync());
    }
}
