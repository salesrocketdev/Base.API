using System.ComponentModel.DataAnnotations;

namespace Base.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }
    [Required]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }
}




