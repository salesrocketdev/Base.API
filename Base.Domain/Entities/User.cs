using System.ComponentModel.DataAnnotations;

namespace Base.Domain.Entities;

public class User : BaseEntity
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;

    public string? AvatarUrl { get; set; }

    // Company relationship (1 user -> 1 company in MVP)
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // Navigation
    public UserCredentials? Credentials { get; set; }
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}




