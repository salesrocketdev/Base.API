using System.ComponentModel.DataAnnotations;

namespace Base.API.DTOs;

public record CompanyCreateRequest(
    [Required] string Name,
    string? SettingsJson
);

public record CompanyUpdateRequest(
    [Required] string Name,
    string? SettingsJson
);

public record CompanyResponse(Guid Id, string Name, string? SettingsJson, DateTime CreatedAt);

