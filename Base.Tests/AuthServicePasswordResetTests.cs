using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Base.Core.Email;
using Base.Core.Email.Models;
using Base.Core.Security;
using Base.Application.Services;
using Base.Domain.Constants;
using Base.Domain.Entities;
using Base.Infrastructure;
using Base.Infrastructure.Data;

namespace Base.Tests;

public class AuthServicePasswordResetTests
{
    [Fact]
    public async Task InitiatePasswordResetAsync_GeneratesOtp_InvalidatesPreviousAndSendsOtp()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("otp@test.local", "OldPassword!123");

        await fixture.Service.InitiatePasswordResetAsync(user.Email);
        var firstOtp = fixture.MailService.LastOtp;

        await fixture.Service.InitiatePasswordResetAsync(user.Email);
        var secondOtp = fixture.MailService.LastOtp;

        Assert.NotNull(firstOtp);
        Assert.NotNull(secondOtp);
        Assert.NotEqual(firstOtp, secondOtp);
        Assert.Matches("^\\d{6}$", secondOtp!);
        Assert.Equal(AuthConstants.PasswordResetOtpLifetimeMinutes, fixture.MailService.LastExpirationMinutes);

        var tokens = await fixture.Db.PasswordResetTokens
            .Where(t => t.UserId == user.Id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);
        Assert.True(tokens[0].IsUsed);
        Assert.NotNull(tokens[0].UsedAt);
        Assert.False(tokens[1].IsUsed);
        Assert.Equal(fixture.OtpProtector.HashOtp(user.Id, secondOtp!), tokens[1].TokenHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidOtp_UpdatesPasswordAndRevokesRefreshTokens()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("reset@test.local", "OldPassword!123");

        var otp = "123456";
        await fixture.Db.PasswordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = fixture.OtpProtector.HashOtp(user.Id, otp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(20)
        });

        await fixture.Db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = fixture.TokenHasher.HashToken("refresh-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        const string newPassword = "NewPassword!123";
        await fixture.Service.ResetPasswordAsync(user.Email, otp, newPassword);

        var credentials = await fixture.Db.UserCredentials.FirstAsync(c => c.UserId == user.Id);
        var resetToken = await fixture.Db.PasswordResetTokens.FirstAsync(t => t.UserId == user.Id);
        var refreshToken = await fixture.Db.RefreshTokens.FirstAsync(t => t.UserId == user.Id);

        Assert.StartsWith("$argon2id$", credentials.PasswordHash);
        Assert.True(fixture.PasswordHasher.VerifyPassword(credentials.PasswordHash, newPassword).Succeeded);
        Assert.True(resetToken.IsUsed);
        Assert.NotNull(resetToken.UsedAt);
        Assert.True(refreshToken.IsRevoked);
        Assert.NotNull(refreshToken.RevokedAt);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResetPasswordAsync(user.Email, otp, "AnotherPassword!123"));
    }

    [Fact]
    public async Task LoginAsync_WithLegacyPbkdf2Password_RehashesToArgon2id()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("legacy@test.local", "OldPassword!123", useLegacyHash: true);

        var credentialsBeforeLogin = await fixture.Db.UserCredentials.AsNoTracking().FirstAsync(c => c.UserId == user.Id);
        Assert.False(credentialsBeforeLogin.PasswordHash.StartsWith("$argon2id$", StringComparison.Ordinal));

        await fixture.Service.LoginAsync(user.Email, "OldPassword!123", "127.0.0.1", "test-agent");

        var credentialsAfterLogin = await fixture.Db.UserCredentials.AsNoTracking().FirstAsync(c => c.UserId == user.Id);
        Assert.StartsWith("$argon2id$", credentialsAfterLogin.PasswordHash);
        Assert.True(fixture.PasswordHasher.VerifyPassword(credentialsAfterLogin.PasswordHash, "OldPassword!123").Succeeded);
    }

    [Fact]
    public async Task CompleteFirstAccessAsync_AnonymousFlow_UpdatesUserAndCreatesSession()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var company = new Company { Name = "first-access-company" };
        await fixture.Db.Companies.AddAsync(company);
        await fixture.Db.SaveChangesAsync();

        var user = new User
        {
            Email = "first.access@test.local",
            CompanyId = company.Id
        };

        await fixture.Db.Users.AddAsync(user);
        await fixture.Db.SaveChangesAsync();

        await fixture.Db.CompanyMembers.AddAsync(new CompanyMember
        {
            CompanyId = company.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow
        });
        await fixture.Db.PasswordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = fixture.OtpProtector.HashOtp(user.Id, "654321"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(20)
        });

        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var (updatedUser, accessToken, refreshToken) = await fixture.Service.CompleteFirstAccessAsync(
            user.Email,
            "654321",
            "NewPassword!123",
            "Jane",
            "Doe",
            "127.0.0.1",
            "test-agent");

        Assert.Equal("Jane Doe", updatedUser.Name);
        Assert.Equal("access-token", accessToken);
        Assert.Equal("refresh-token", refreshToken);

        var credentials = await fixture.Db.UserCredentials.AsNoTracking().FirstAsync(c => c.UserId == updatedUser.Id);
        Assert.True(fixture.PasswordHasher.VerifyPassword(credentials.PasswordHash, "NewPassword!123").Succeeded);

        var persistedUser = await fixture.Db.Users.AsNoTracking().FirstAsync(u => u.Id == updatedUser.Id);
        Assert.Equal("Jane Doe", persistedUser.Name);
        Assert.False(string.IsNullOrWhiteSpace(persistedUser.AvatarUrl));

        var resetToken = await fixture.Db.PasswordResetTokens.AsNoTracking().FirstAsync(t => t.UserId == updatedUser.Id);
        Assert.True(resetToken.IsUsed);

        var storedRefreshToken = await fixture.Db.RefreshTokens.AsNoTracking().FirstAsync(t => t.UserId == updatedUser.Id);
        Assert.Equal(fixture.TokenHasher.HashToken("refresh-token"), storedRefreshToken.Token);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public ApplicationDbContext Db { get; }
        public AuthService Service { get; }
        public CapturingSendMailService MailService { get; }
        public Sha256TokenHasher TokenHasher { get; }
        public IPasswordResetOtpProtector OtpProtector { get; }
        public IPasswordHasher PasswordHasher { get; }
        public Pbkdf2PasswordHasher LegacyPasswordHasher { get; }

        private TestFixture(
            SqliteConnection connection,
            ApplicationDbContext db,
            AuthService service,
            CapturingSendMailService mailService,
            Sha256TokenHasher tokenHasher,
            IPasswordResetOtpProtector otpProtector,
            IPasswordHasher passwordHasher,
            Pbkdf2PasswordHasher legacyPasswordHasher)
        {
            _connection = connection;
            Db = db;
            Service = service;
            MailService = mailService;
            TokenHasher = tokenHasher;
            OtpProtector = otpProtector;
            PasswordHasher = passwordHasher;
            LegacyPasswordHasher = legacyPasswordHasher;
        }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:PasswordHashing:TimeCost"] = "1",
                    ["Security:PasswordHashing:MemoryCost"] = "1024",
                    ["Security:PasswordHashing:Lanes"] = "1",
                    ["Security:PasswordHashing:Threads"] = "1",
                    ["Security:PasswordHashing:HashLength"] = "16",
                    ["Security:PasswordHashing:SaltLength"] = "16",
                    ["Security:PasswordHashing:Pepper"] = "test-password-pepper-1234567890",
                    ["Security:PasswordHashing:Iterations"] = "1000",
                    ["Security:PasswordReset:Pepper"] = "test-pepper-1234567890"
                })
                .Build();

            var legacyPasswordHasher = new Pbkdf2PasswordHasher(configuration);
            var passwordHasher = new HybridPasswordHasher(new Argon2idPasswordHasher(configuration), legacyPasswordHasher);
            var tokenHasher = new Sha256TokenHasher();
            var otpProtector = new PasswordResetOtpProtector(configuration);
            var mail = new CapturingSendMailService();
            var service = new AuthService(
                new UnitOfWork(db),
                passwordHasher,
                new FakeJwtTokenGenerator(),
                tokenHasher,
                otpProtector,
                mail);

            return new TestFixture(connection, db, service, mail, tokenHasher, otpProtector, passwordHasher, legacyPasswordHasher);
        }

        public async Task<User> CreateUserAsync(string email, string password, bool useLegacyHash = false)
        {
            var company = new Company { Name = Guid.NewGuid().ToString("N") };
            await Db.Companies.AddAsync(company);
            await Db.SaveChangesAsync();

            var user = new User
            {
                Email = email,
                Name = "Test User",
                CompanyId = company.Id
            };

            await Db.Users.AddAsync(user);
            await Db.SaveChangesAsync();

            await Db.UserCredentials.AddAsync(new UserCredentials
            {
                UserId = user.Id,
                PasswordHash = useLegacyHash
                    ? LegacyPasswordHasher.HashPassword(password)
                    : PasswordHasher.HashPassword(password)
            });

            await Db.CompanyMembers.AddAsync(new CompanyMember
            {
                CompanyId = company.Id,
                UserId = user.Id,
                Role = "Owner",
                CreatedAt = DateTime.UtcNow
            });

            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public string GenerateAccessToken(int userId, string email) => "access-token";

        public string GenerateRefreshToken() => "refresh-token";
    }

    private sealed class CapturingSendMailService : ISendMailService
    {
        public string? LastOtp { get; private set; }
        public int? LastExpirationMinutes { get; private set; }

        public void EnqueueWelcomeEmail(string toEmail, WelcomeModel model)
        {
        }

        public void EnqueueVerificationCodeEmail(string toEmail, VerificationCodeModel model)
        {
            LastOtp = model.OTP;
            LastExpirationMinutes = model.ExpirationMinutes;
        }

        public Task<bool> SendWelcomeEmail(string email, WelcomeModel model) => Task.FromResult(true);

        public Task<bool> SendVerificationCodeEmail(string email, VerificationCodeModel model) => Task.FromResult(true);
    }
}
