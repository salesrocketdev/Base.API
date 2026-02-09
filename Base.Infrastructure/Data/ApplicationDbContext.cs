using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Base.Domain.Entities;
using System.Text.Json;

namespace Base.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

            if (string.IsNullOrWhiteSpace(conn))
            {
                throw new InvalidOperationException("Database connection string not configured for design-time. Set 'ConnectionStrings__DefaultConnection' or 'DEFAULT_CONNECTION' environment variable.");
            }

            optionsBuilder.UseNpgsql(conn);
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserCredentials> UserCredentials => Set<UserCredentials>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AppLog> AppLogs => Set<AppLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMember> CompanyMembers => Set<CompanyMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(100);
            entity.Property(u => u.CreatedAt).IsRequired();
            entity.Property(u => u.IsActive).IsRequired();
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.HasOne(u => u.Credentials).WithOne(c => c.User).HasForeignKey<UserCredentials>(c => c.UserId);
            entity.HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserCredentials>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.PasswordHash).IsRequired();
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.HasOne(c => c.User).WithOne(u => u.Credentials).HasForeignKey<UserCredentials>(c => c.UserId);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.EventType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.IpAddress).HasMaxLength(45);
            entity.Property(a => a.UserAgent).HasMaxLength(500);
            entity.Property(a => a.Timestamp).IsRequired();
            entity.HasOne(a => a.User).WithMany(u => u.AuditEvents).HasForeignKey(a => a.UserId);
        });

        modelBuilder.Entity<AppLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Level).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(l => l.Category).HasMaxLength(100);
            entity.Property(l => l.Message).IsRequired().HasMaxLength(2000);
            entity.Property(l => l.ExceptionType).HasMaxLength(200);
            entity.Property(l => l.ExceptionMessage).HasMaxLength(4000);
            entity.Property(l => l.TraceId).HasMaxLength(100);
            entity.Property(l => l.CorrelationId).HasMaxLength(100);
            entity.Property(l => l.RequestPath).HasMaxLength(300);
            entity.Property(l => l.RequestMethod).HasMaxLength(10);
            entity.Property(l => l.Source).HasMaxLength(200);
            entity.Property(l => l.Provider).HasMaxLength(100);
            entity.Property(l => l.CreatedAt).IsRequired();
            entity.HasIndex(l => l.CreatedAt);
            entity.HasIndex(l => l.Level);
            entity.HasIndex(l => l.CompanyId);
            entity.HasIndex(l => l.UserId);
            entity.HasIndex(l => l.CorrelationId);
            entity.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(l => l.Company).WithMany().HasForeignKey(l => l.CompanyId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).IsRequired();
            entity.Property(r => r.DeviceInfo).HasMaxLength(500);
            entity.Property(r => r.ExpiresAt).IsRequired();
            entity.Property(r => r.CreatedAt).IsRequired();
            entity.Property(r => r.IsRevoked).IsRequired();
            entity.HasOne(r => r.User).WithMany(u => u.RefreshTokens).HasForeignKey(r => r.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TokenHash).IsRequired();
            entity.Property(p => p.ExpiresAt).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.Property(p => p.IsUsed).IsRequired();
            entity.HasOne(p => p.User).WithMany(u => u.PasswordResetTokens).HasForeignKey(p => p.UserId);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.CreatedAt).IsRequired();

            var jsonConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<JsonDocument?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v),
                v => v == null ? null : JsonDocument.Parse(v));

            var jsonComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<JsonDocument?>(
                (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.RootElement.GetRawText() == b.RootElement.GetRawText()),
                v => v == null ? 0 : v.RootElement.GetRawText().GetHashCode(),
                v => v == null ? null : JsonDocument.Parse(v.RootElement.GetRawText()));

            var settingsProp = entity.Property(c => c.Settings).HasConversion(jsonConverter);
            settingsProp.Metadata.SetValueComparer(jsonComparer);

            if (Database.ProviderName?.Contains("Npgsql") == true)
            {
                settingsProp.HasColumnType("jsonb");
            }
        });

        modelBuilder.Entity<CompanyMember>(entity =>
        {
            entity.HasKey(cm => cm.Id);
            entity.Property(cm => cm.Role).IsRequired().HasMaxLength(50);
            entity.Property(cm => cm.CreatedAt).IsRequired();
            entity.HasIndex(cm => new { cm.CompanyId, cm.UserId }).IsUnique();

            entity.HasOne(cm => cm.Company)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cm => cm.User)
                .WithOne()
                .HasForeignKey<CompanyMember>(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ApplyBaseEntityConfiguration(modelBuilder);
    }

    private void ApplyBaseEntityConfiguration(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var entity = modelBuilder.Entity(entityType.ClrType);

            entity.Property(nameof(BaseEntity.PublicId)).IsRequired();

            if (Database.ProviderName?.Contains("Npgsql") == true)
            {
                entity.Property(nameof(BaseEntity.PublicId))
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");
            }

            entity.HasIndex(nameof(BaseEntity.PublicId)).IsUnique();
            entity.Property(nameof(BaseEntity.IsDeleted)).IsRequired();
            entity.Property(nameof(BaseEntity.DeletedAt));
            entity.Property(nameof(BaseEntity.UpdatedAt));
            entity.Property(nameof(BaseEntity.CreatedAt)).IsRequired();

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Equal(isDeletedProperty, System.Linq.Expressions.Expression.Constant(false)),
                parameter);

            entity.HasQueryFilter(filter);
        }
    }
}

public class ApplicationDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                   ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

        if (string.IsNullOrWhiteSpace(conn))
        {
            var basePaths = new[]
            {
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), "Base.API"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "Base.API")
            };

            foreach (var basePath in basePaths)
            {
                if (!Directory.Exists(basePath))
                {
                    continue;
                }

                try
                {
                    var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                        .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true, reloadOnChange: false)
                        .AddJsonFile(Path.Combine(basePath, "appsettings.Development.json"), optional: true, reloadOnChange: false)
                        .AddEnvironmentVariables()
                        .Build();

                    conn = configuration.GetConnectionString("DefaultConnection")
                           ?? configuration["DEFAULT_CONNECTION"];
                }
                catch
                {
                }

                if (!string.IsNullOrWhiteSpace(conn))
                {
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException("Design-time DbContext requires a connection string. Set 'ConnectionStrings__DefaultConnection' or 'DEFAULT_CONNECTION' environment variable.");
        }

        optionsBuilder.UseNpgsql(conn);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

