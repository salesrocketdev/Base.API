using Base.Core.Tenant;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;
using Base.Infrastructure.Repositories;

namespace Base.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private IUserRepository? _users;
    private IUserCredentialsRepository? _userCredentials;
    private IRefreshTokenRepository? _refreshTokens;
    private IPasswordResetTokenRepository? _passwordResetTokens;
    private IAuditEventRepository? _auditEvents;
    private IAppLogRepository? _appLogs;
    private ICompanyRepository? _companies;
    private ICompanyMemberRepository? _companyMembers;

    public UnitOfWork(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public IUserRepository Users => _users ??= new TenantScopedUserRepository(_context, _tenantContext);
    public IUserCredentialsRepository UserCredentials => _userCredentials ??= new UserCredentialsRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IPasswordResetTokenRepository PasswordResetTokens => _passwordResetTokens ??= new PasswordResetTokenRepository(_context);
    public IAuditEventRepository AuditEvents => _auditEvents ??= new AuditEventRepository(_context);
    public IAppLogRepository AppLogs => _appLogs ??= new AppLogRepository(_context);
    public ICompanyRepository Companies => _companies ??= new CompanyRepository(_context);
    public ICompanyMemberRepository CompanyMembers => _companyMembers ??= new CompanyMemberRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}


