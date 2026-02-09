using System.ComponentModel.DataAnnotations;

namespace Base.Domain.Entities;

public class RefreshToken : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }
    [Required]
    public string Token { get; set; } = string.Empty; // Hashed token

    public string? DeviceInfo { get; set; }

    public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }
}




