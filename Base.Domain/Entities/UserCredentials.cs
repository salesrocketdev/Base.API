using System.ComponentModel.DataAnnotations;

namespace Base.Domain.Entities;

public class UserCredentials : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

        public DateTime? LastPasswordChange { get; set; }
}




