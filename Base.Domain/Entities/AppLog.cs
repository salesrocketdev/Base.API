using Microsoft.Extensions.Logging;
namespace Base.Domain.Entities;

public class AppLog
{
    public long Id { get; set; }
    public LogLevel Level { get; set; } = LogLevel.Error;
    public string? Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? Details { get; set; }
    public int? UserId { get; set; }
    public int? CompanyId { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? Source { get; set; }
    public string? Provider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Company? Company { get; set; }
}


