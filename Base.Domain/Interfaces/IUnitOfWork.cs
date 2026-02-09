namespace Base.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IUserCredentialsRepository UserCredentials { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    IAuditEventRepository AuditEvents { get; }
    IAppLogRepository AppLogs { get; }
    ICompanyRepository Companies { get; }
    ICompanyMemberRepository CompanyMembers { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}


