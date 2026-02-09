using System.ComponentModel.DataAnnotations;

namespace Base.Domain.Entities;

public class AuditEvent : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }
    [Required]
    public string EventType { get; set; } = string.Empty; // signup, login, logout, reset

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }
}




